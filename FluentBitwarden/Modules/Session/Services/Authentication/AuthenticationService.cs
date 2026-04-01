using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using BitwardenApi.Modules.Identity.Abstractions;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using BitwardenApi.Shared.Cryptography;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Modules.Session.Models.Authentication;

namespace FluentBitwarden.Modules.Session.Services.Authentication;

internal sealed class AuthenticationService(IIdentityApiClient identityApiClient) : IAuthenticationService
{
    private static readonly JwtSecurityTokenHandler JwtSecurityTokenHandler = new();

    public async Task<PasswordSignInOutcome> SignInWithPasswordAsync(
        BitwardenClientContext context,
        string email,
        string masterPassword,
        CancellationToken cancellationToken = default)
    {
        string serverAuthorizationHash =
            SessionCrypto.HashMasterPassword(email, masterPassword, new KdfConfig.Pbkdf2(600000));

        var passwordLoginRequest = new PasswordLoginRequest(context, email, serverAuthorizationHash);

        var result = await identityApiClient.LoginWithPasswordAsync(passwordLoginRequest, cancellationToken);
        return ParseTokenOutcome(email, serverAuthorizationHash, result);
    }

    public async Task<PasswordSignInOutcome> ContinueTwoFactorAsync(
        BitwardenClientContext context,
        string email,
        string serverAuthorizationHash,
        TwoFactorProof twoFactorProof,
        CancellationToken cancellationToken)
    {
        var result = await identityApiClient.LoginWithPasswordAndTwoFactorAsync(new PasswordTwoFactorLoginRequest(context,
            email, serverAuthorizationHash, twoFactorProof), cancellationToken);

        return ParseTokenOutcome(email, serverAuthorizationHash, result);
    }

    private static PasswordSignInOutcome ParseTokenOutcome(
        string email,
        string serverAuthorizationHash,
        TokenExchangeOutcome outcome) => outcome switch
    {
        TokenExchangeOutcome.Authenticated success => new PasswordSignInOutcome.Success(
            CreateAuthenticationSuccess(success.AuthenticatedModel)),
        TokenExchangeOutcome.DeviceVerificationRequired dv => new PasswordSignInOutcome.DeviceVerificationRequired(
            dv.Message),
        TokenExchangeOutcome.InvalidCredentials ic => new PasswordSignInOutcome.InvalidCredentials(ic.Message),
        TokenExchangeOutcome.TwoFactorRequired twoFactorRequired => new PasswordSignInOutcome.TwoFactorRequired(
            twoFactorRequired.Challenge, email, serverAuthorizationHash),
        _ => throw new InvalidOperationException("Unsupported password token outcome.")
    };

    private static AuthenticationSuccess CreateAuthenticationSuccess(TokenAuthenticatedModel model)
    {
        var jwt = JwtSecurityTokenHandler.ReadJwtToken(model.AccessToken.Value);
        string accountId = jwt.Claims.First(c => c.Type == "sub").Value;
        string email = jwt.Claims.First(c => c.Type == "email").Value;

        return new AuthenticationSuccess(UserId.Parse(accountId), email,
            new SessionTokens(model.RefreshToken, model.TwoFactorToken, model.AccessToken, model.ExpiresAt),
            new AccountCryptoMaterial(model.MasterPasswordUnlockModel.KdfConfig,
                model.MasterPasswordUnlockModel.UserKey, model.PrivateKey));
    }
}
