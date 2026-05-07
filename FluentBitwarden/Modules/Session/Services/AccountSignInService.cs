using BitwardenApi.Cryptography;
using BitwardenApi.Modules.Identity.Abstractions;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace FluentBitwarden.Modules.Session.Services;

internal sealed class AccountSignInService(IIdentityApiClient identityApiClient) : IAccountSignInService
{
    public async Task<AccountSignInOutcome> SignInWithPasswordAsync(AccountSignInRequest.PasswordRequest request, CancellationToken cancellationToken = default)
    {
        string serverAuthorizationHash =
            CryptographyService.HashMasterPassword(request.Email, request.MasterPassword, new KdfConfig.Pbkdf2(600000));

        var passwordLoginRequest = new PasswordLoginRequest(request.Context, request.Email, serverAuthorizationHash);

        var result = await identityApiClient.LoginWithPasswordAsync(passwordLoginRequest, cancellationToken);
        return ParseTokenOutcome(request.Email, serverAuthorizationHash, result, request.Context.Environment);
    }

    public async Task<AccountSignInOutcome> SignInWithTwoFactorAsync(AccountSignInRequest.TwoFactorRequest request, CancellationToken cancellationToken)
    {
        var result = await identityApiClient.LoginWithPasswordAndTwoFactorAsync(
            new PasswordTwoFactorLoginRequest(request.Context,
                request.Email, request.ServerAuthorizationHash, request.TwoFactorProof), cancellationToken);

        return ParseTokenOutcome(request.Email, request.ServerAuthorizationHash, result, request.Context.Environment);
    }

    private static AccountSignInOutcome ParseTokenOutcome(
        string email,
        string serverAuthorizationHash,
        TokenExchangeOutcome outcome,
        BitwardenEnvironment environment) => outcome switch
    {
        TokenExchangeOutcome.Authenticated success => new AccountSignInOutcome.Success(
            CreateAuthenticationSuccess(success.AuthenticatedModel, environment)),
        TokenExchangeOutcome.DeviceVerificationRequired dv =>
            new AccountSignInOutcome.DeviceVerificationRequired(dv.Message),
        TokenExchangeOutcome.InvalidCredentials ic => new AccountSignInOutcome.InvalidCredentials(ic.Message),
        TokenExchangeOutcome.TwoFactorRequired twoFactorRequired => new AccountSignInOutcome.TwoFactorRequired(
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