using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using BitwardenApi.Identity;
using BitwardenApi.Identity.Contracts;
using BitwardenApi.Infrastructure.Cryptography;
using BitwardenApi.Primitives;
using FluentBitwarden.AppHost.Infrastructure.WebAuthn;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Contracts.Modules.Accounts.Login;

namespace FluentBitwarden.AppHost.Modules.Account.Services;

internal sealed class AccountAuthenticatorService(
    IIdentityApi identityApi,
    IWebAuthnIdentityApi webAuthnIdentityApi)
{
    public abstract record Result
    {
        private Result() { }

        public sealed record Authenticated(
            AccountProfile Profile,
            AccountKeyMaterial KeyMaterial,
            RefreshToken RefreshToken) : Result;

        public sealed record Rejected(AccountAuthenticationOutcome Outcome) : Result;
    }


    public Task<Result> AuthenticateAsync(AccountAuthenticationRequest request, CancellationToken cancellationToken) =>
        request switch
        {
            AccountAuthenticationRequest.Password password =>
                AuthenticateWithPasswordAsync(password, cancellationToken),

            AccountAuthenticationRequest.Passkey passkey =>
                AuthenticateWithPasskeyAsync(passkey, cancellationToken),

            AccountAuthenticationRequest.TwoFactor twoFactor =>
                AuthenticateWithTwoFactorAsync(twoFactor, cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

    private async Task<Result> AuthenticateWithPasswordAsync(
        AccountAuthenticationRequest.Password request,
        CancellationToken cancellationToken)
    {
        var serverAuthorizationHash = MasterPassword.HashMasterPassword(
            request.Email,
            request.MasterPassword,
            new KdfConfig.Pbkdf2(600000)).Value;

        var result = await identityApi.AuthenticateWithPasswordAsync(
            new PasswordAuthenticationRequest(
                request.Context,
                request.Email,
                serverAuthorizationHash),
            cancellationToken);

        return MapAuthenticationResult(
            result,
            request.Context.Environment,
            request.Email,
            serverAuthorizationHash);
    }

    private async Task<Result> AuthenticateWithTwoFactorAsync(
        AccountAuthenticationRequest.TwoFactor request,
        CancellationToken cancellationToken)
    {
        var result =
            await identityApi.AuthenticateWithPasswordAndTwoFactorAsync(
                new PasswordTwoFactorAuthenticationRequest(
                    request.Context,
                    request.Email,
                    request.ServerAuthorizationHash,
                    request.TwoFactorProof),
                cancellationToken);

        return MapAuthenticationResult(
            result,
            request.Context.Environment,
            request.Email,
            request.ServerAuthorizationHash);
    }

    private async Task<Result> AuthenticateWithPasskeyAsync(
        AccountAuthenticationRequest.Passkey request,
        CancellationToken cancellationToken)
    {
        try
        {
            var assertionOptions =
                await webAuthnIdentityApi.GetAuthenticationAssertionOptionsAsync(
                    request.Context,
                    cancellationToken);

            var assertion =
                WebAuthnLoginAssertionHelper.GetAssertion(
                    assertionOptions.Options,
                    request.OwerHwnd);

            var result = await identityApi.AuthenticateWithWebAuthnAsync(
                new WebAuthnAuthenticationRequest(
                    request.Context,
                    assertionOptions.Token,
                    assertion),
                cancellationToken);

            return MapAuthenticationResult(
                result,
                request.Context.Environment);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Reject(new AccountAuthenticationOutcome.InvalidCredentials("Passkey sign in was canceled."));
        }
        catch (WebAuthnLoginException ex)
        {
            return Reject(new AccountAuthenticationOutcome.InvalidCredentials(ex.Message));
        }
    }

    private static Result MapAuthenticationResult(
        SessionTokenResult<TokenAuthenticatedModel> result,
        BitwardenEnvironment environment,
        string? email = null,
        string? serverAuthorizationHash = null)
    {
        return result switch
        {
            SessionTokenResult<TokenAuthenticatedModel>.Success success =>
                CreateAuthenticatedResult(
                    success.Value,
                    environment),

            SessionTokenResult<TokenAuthenticatedModel>.Rejected rejected =>
                Reject(
                    MapRejection(
                        rejected.Error,
                        email,
                        serverAuthorizationHash)),

            _ => throw new InvalidOperationException(
                $"Unsupported authentication result: {result.GetType().Name}.")
        };
    }

    private static AccountAuthenticationOutcome MapRejection(
        SessionTokenRejection rejection,
        string? email,
        string? serverAuthorizationHash)
    {
        return rejection.Kind switch
        {
            SessionTokenRejectionKind.InvalidCredentials =>
                new AccountAuthenticationOutcome.InvalidCredentials(
                    rejection.Message),

            SessionTokenRejectionKind.DeviceVerificationRequired =>
                new AccountAuthenticationOutcome.DeviceVerificationRequired(
                    rejection.Message),

            SessionTokenRejectionKind.TwoFactorRequired
                when rejection.TwoFactorChallenge is not null
                     && email is not null
                     && serverAuthorizationHash is not null =>
                new AccountAuthenticationOutcome.TwoFactorRequired(
                    rejection.TwoFactorChallenge ?? throw new InvalidDataException("Two-factor challenge is missing."),
                    email,
                    serverAuthorizationHash),

            SessionTokenRejectionKind.TwoFactorRequired =>
                throw new InvalidDataException(
                    "Identity returned a two-factor challenge without " +
                    "the required password authentication context."),

            _ => throw new InvalidOperationException(
                $"Unsupported authentication rejection: {rejection.Kind}.")
        };
    }

    private static Result.Authenticated CreateAuthenticatedResult(
        TokenAuthenticatedModel model,
        BitwardenEnvironment environment)
    {
        var jwt = new JwtSecurityTokenHandler()
            .ReadJwtToken(model.AccessToken.ToString());

        var accountId = jwt.GetRequiredClaim("sub");
        var email = jwt.GetRequiredClaim("email");

        var userId = UserId.Parse(
            accountId,
            CultureInfo.InvariantCulture);

        var profile = new AccountProfile(
            userId,
            email,
            environment);

        var keyMaterial = new AccountKeyMaterial(
            userId,
            model.MasterPasswordUnlockModel.Salt,
            model.MasterPasswordUnlockModel.KdfConfig,
            model.MasterPasswordUnlockModel.UserKey,
            model.PrivateKey);

        return new Result.Authenticated(
            profile,
            keyMaterial,
            model.RefreshToken);
    }

    private static Result.Rejected Reject(
        AccountAuthenticationOutcome outcome)
        => new(outcome);
}
