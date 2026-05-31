using BitwardenApi.Contracts;
using BitwardenApi.Cryptography;
using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.Infrastructure.Security.WebAuthn;
using System.IdentityModel.Tokens.Jwt;
using FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Models;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts.Models;
using FluentBitwarden.Contracts.Infrastructure.Shared;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

namespace FluentBitwarden.AppHost.Modules.Accounts.Login;

using AccountLoginOperationResult = OperationResult<AccountLoginOutcome, AccountLoginSuccess>;

[Fody.ConfigureAwait(false)]
internal sealed class AccountLoginService(
    IUnitOfWorkFactory unitOfWorkFactory,
    IIdentityApiClient identityApiClient) : IAccountLoginService
{
    public async ValueTask<AccountLoginOutcome> LoginAsync(AccountLoginRequest request, CancellationToken cancellationToken)
    {
        Task<AccountLoginOperationResult> signInTask = request switch
        {
            AccountLoginRequest.PasswordRequest passwordRequest => LoginWithPasswordAsync(passwordRequest, cancellationToken),
            AccountLoginRequest.PasskeyRequest passkeyRequest => LoginWithPasskeyAsync(passkeyRequest, cancellationToken),
            AccountLoginRequest.TwoFactorRequest twoFactorRequest => LoginWithTwoFactorAsync(twoFactorRequest, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        var result = await signInTask;
        if (!result.TryGetPayload(out var accountSignIn))
            return result.Outcome;

        using var unitOfWork = unitOfWorkFactory.Create();

        unitOfWork.AccountProfileRepository.Upsert(new AccountProfile(
            accountSignIn.UserId,
            accountSignIn.Email,
            accountSignIn.Environment,
            LastSyncAt: DateTimeOffset.MinValue));

        unitOfWork.AccountKeyMaterialRepository.Upsert(accountSignIn.AccountKeyMaterial);
        unitOfWork.SaveChanges();

        unitOfWork.SecureRefreshTokenStore.Store(accountSignIn.UserId, accountSignIn.AuthenticationTokens.RefreshToken);
        return new AccountLoginOutcome.Success();
    }

    private async Task<AccountLoginOperationResult> LoginWithPasswordAsync(AccountLoginRequest.PasswordRequest request, CancellationToken cancellationToken = default)
    {
        string serverAuthorizationHash =
            MasterPassword.HashMasterPassword(request.Email, request.MasterPassword, new KdfConfig.Pbkdf2(600000));

        var passwordLoginRequest = new PasswordLoginRequest(request.Context, request.Email, serverAuthorizationHash);

        var result = await identityApiClient.LoginWithPasswordAsync(passwordLoginRequest, cancellationToken);
        return ParseTokenOutcome(request.Email, serverAuthorizationHash, result, request.Context.Environment);
    }

    private async Task<AccountLoginOperationResult> LoginWithPasskeyAsync(
        AccountLoginRequest.PasskeyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var assertionOptions = await identityApiClient.GetWebAuthnLoginAssertionOptionsAsync(
                request.Context,
                cancellationToken);

            var deviceResponse = WebAuthnLoginAssertionHelper.GetAssertion(
                assertionOptions.Options,
                request.OwerHwnd);

            var result = await identityApiClient.LoginWithWebAuthnAsync(
                new WebAuthnLoginRequest(request.Context, assertionOptions.Token, deviceResponse),
                cancellationToken);

            return ParseTokenOutcome(string.Empty, string.Empty, result, request.Context.Environment);
        }
        catch (OperationCanceledException)
        {
            return AccountLoginOperationResult.WithoutPayload(new AccountLoginOutcome.InvalidCredentials("Passkey sign in was canceled."));
        }
        catch (WebAuthnLoginException ex)
        {
            return AccountLoginOperationResult.WithoutPayload(new AccountLoginOutcome.InvalidCredentials(ex.Message));
        }
    }

    private async Task<AccountLoginOperationResult> LoginWithTwoFactorAsync(AccountLoginRequest.TwoFactorRequest request, CancellationToken cancellationToken)
    {
        var result = await identityApiClient.LoginWithPasswordAndTwoFactorAsync(
            new PasswordTwoFactorLoginRequest(request.Context,
                request.Email, request.ServerAuthorizationHash, request.TwoFactorProof), cancellationToken);

        return ParseTokenOutcome(request.Email, request.ServerAuthorizationHash, result, request.Context.Environment);
    }

    private static AccountLoginOperationResult ParseTokenOutcome(
        string email,
        string serverAuthorizationHash,
        TokenExchangeOutcome outcome,
        BitwardenEnvironment environment) => outcome switch
    {
        TokenExchangeOutcome.Authenticated success => AccountLoginOperationResult.WithPayload(new AccountLoginOutcome.Success(), CreateAuthenticationSuccess(success.AuthenticatedModel, environment)),
        TokenExchangeOutcome.DeviceVerificationRequired dv =>
            AccountLoginOperationResult.WithoutPayload(new AccountLoginOutcome.DeviceVerificationRequired(dv.Message)),
        TokenExchangeOutcome.InvalidCredentials ic => AccountLoginOperationResult.WithoutPayload(new AccountLoginOutcome.InvalidCredentials(ic.Message)),
        TokenExchangeOutcome.TwoFactorRequired twoFactorRequired => AccountLoginOperationResult.WithoutPayload(new AccountLoginOutcome.TwoFactorRequired(
            twoFactorRequired.Challenge, email, serverAuthorizationHash)),
        _ => throw new InvalidOperationException("Unsupported password token outcome.")
    };

    private static AccountLoginSuccess CreateAuthenticationSuccess(TokenAuthenticatedModel model, BitwardenEnvironment environment)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(model.AccessToken.ToString());
        string accountId = jwt.Claims.First(c => c.Type == "sub").Value;
        string email = jwt.Claims.First(c => c.Type == "email").Value;

        var userId = UserId.Parse(accountId);

        return new AccountLoginSuccess(
            UserId.Parse(accountId),
            email,
            new AccountAuthenticationTokens(userId,
                new BitwardenClientContext(environment, DeviceIdentity.DeviceInfo),
                model.RefreshToken, model.AccessToken, model.ExpiresAt),
            new AccountKeyMaterial(
                userId,
                model.MasterPasswordUnlockModel.Salt,
                model.MasterPasswordUnlockModel.KdfConfig,
                model.MasterPasswordUnlockModel.UserKey,
                model.PrivateKey), environment);
    }
}
