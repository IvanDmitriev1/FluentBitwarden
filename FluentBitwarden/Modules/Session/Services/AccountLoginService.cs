using BitwardenApi.Cryptography;
using BitwardenApi.Contracts;
using BitwardenApi.Models;
using FluentBitwarden.Infrastructure.Security;
using FluentBitwarden.Infrastructure.Security.WebAuthn;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace FluentBitwarden.Modules.Session.Services;

[Fody.ConfigureAwait(false)]
internal sealed class AccountLoginService(
    IIdentityApiClient identityApiClient) : IAccountLoginService
{
    public async Task<AccountLoginnOutcome> LoginWithPasswordAsync(AccountLoginRequest.PasswordRequest request, CancellationToken cancellationToken = default)
    {
        string serverAuthorizationHash =
            MasterPassword.HashMasterPassword(request.Email, request.MasterPassword, new KdfConfig.Pbkdf2(600000));

        var passwordLoginRequest = new PasswordLoginRequest(request.Context, request.Email, serverAuthorizationHash);

        var result = await identityApiClient.LoginWithPasswordAsync(passwordLoginRequest, cancellationToken);
        return ParseTokenOutcome(request.Email, serverAuthorizationHash, result, request.Context.Environment);
    }

    public async Task<AccountLoginnOutcome> LoginWithPasskeyAsync(
        AccountLoginRequest.PasskeyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var assertionOptions = await identityApiClient.GetWebAuthnLoginAssertionOptionsAsync(
                request.Context,
                cancellationToken);

            var deviceResponse = WebAuthnLoginAssertionHelper.GetAssertion(assertionOptions.Options);

            var result = await identityApiClient.LoginWithWebAuthnAsync(
                new WebAuthnLoginRequest(request.Context, assertionOptions.Token, deviceResponse),
                cancellationToken);

            return ParseTokenOutcome(string.Empty, string.Empty, result, request.Context.Environment);
        }
        catch (OperationCanceledException)
        {
            return new AccountLoginnOutcome.InvalidCredentials("Passkey sign in was canceled.");
        }
        catch (WebAuthnLoginException ex)
        {
            return new AccountLoginnOutcome.InvalidCredentials(ex.Message);
        }
    }

    public async Task<AccountLoginnOutcome> LoginWithTwoFactorAsync(AccountLoginRequest.TwoFactorRequest request, CancellationToken cancellationToken)
    {
        var result = await identityApiClient.LoginWithPasswordAndTwoFactorAsync(
            new PasswordTwoFactorLoginRequest(request.Context,
                request.Email, request.ServerAuthorizationHash, request.TwoFactorProof), cancellationToken);

        return ParseTokenOutcome(request.Email, request.ServerAuthorizationHash, result, request.Context.Environment);
    }

    private static AccountLoginnOutcome ParseTokenOutcome(
        string email,
        string serverAuthorizationHash,
        TokenExchangeOutcome outcome,
        BitwardenEnvironment environment) => outcome switch
    {
        TokenExchangeOutcome.Authenticated success => new AccountLoginnOutcome.Success(
            CreateAuthenticationSuccess(success.AuthenticatedModel, environment)),
        TokenExchangeOutcome.DeviceVerificationRequired dv =>
            new AccountLoginnOutcome.DeviceVerificationRequired(dv.Message),
        TokenExchangeOutcome.InvalidCredentials ic => new AccountLoginnOutcome.InvalidCredentials(ic.Message),
        TokenExchangeOutcome.TwoFactorRequired twoFactorRequired => new AccountLoginnOutcome.TwoFactorRequired(
            twoFactorRequired.Challenge, email, serverAuthorizationHash),
        _ => throw new InvalidOperationException("Unsupported password token outcome.")
    };

    private static AccountSignInSuccess CreateAuthenticationSuccess(TokenAuthenticatedModel model, BitwardenEnvironment environment)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(model.AccessToken.ToString());
        string accountId = jwt.Claims.First(c => c.Type == "sub").Value;
        string email = jwt.Claims.First(c => c.Type == "email").Value;

        var userId = UserId.Parse(accountId);

        return new AccountSignInSuccess(
            UserId.Parse(accountId),
            email,
            new AccountSessionTokens(model.RefreshToken, model.AccessToken, model.ExpiresAt),
            new AccountKeyMaterial(
                userId,
                model.MasterPasswordUnlockModel.Salt,
                model.MasterPasswordUnlockModel.KdfConfig,
                model.MasterPasswordUnlockModel.UserKey,
                model.PrivateKey), environment);
    }

}
