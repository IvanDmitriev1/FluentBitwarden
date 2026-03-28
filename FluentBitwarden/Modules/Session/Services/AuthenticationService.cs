using BitwardenApi.Modules.Identity.Abstractions;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Cryptography;
using FluentBitwarden.Modules.Session.Abstractions;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Modules.Session.Models.Authentication;

namespace FluentBitwarden.Modules.Session.Services;

internal sealed class AuthenticationService(IIdentityApiClient identityApiClient) : IAuthenticationService
{
    private static readonly JwtSecurityTokenHandler JwtSecurityTokenHandler = new();

    public async Task<PasswordSignInOutcome> SignInWithPasswordAsync(PasswordSignInRequest request, CancellationToken cancellationToken = default)
    {
        var signInContinuation = SessionCrypto.DeriveMasterPasswordAuth(request.Email, request.MasterPassword, new KdfConfig.Pbkdf2(600000));

        var passwordLoginRequest =
            new PasswordLoginRequest(request.Context, request.Email, signInContinuation.ServerAuthorizationHash);

        var result = await identityApiClient.LoginWithPasswordAsync(passwordLoginRequest, cancellationToken);
        return ParseTokenOutcome(signInContinuation, result);
    }

    public async Task<PasswordSignInOutcome> ContinueTwoFactorAsync(
        BitwardenClientContext context,
        PasswordSignInContinuation passwordSignInContinuation,
        TwoFactorProof twoFactorProof,
        CancellationToken cancellationToken)
    {
         var result = await identityApiClient.LoginWithPasswordAndTwoFactorAsync(new PasswordTwoFactorLoginRequest(context,
            passwordSignInContinuation.Email, passwordSignInContinuation.ServerAuthorizationHash, twoFactorProof), cancellationToken);

         return ParseTokenOutcome(passwordSignInContinuation, result);
    }


    private static PasswordSignInOutcome ParseTokenOutcome(PasswordSignInContinuation signInContinuation, TokenExchangeOutcome outcome)
    {
        PasswordSignInContinuation? currentContinuation = signInContinuation;

        try
        {
            switch (outcome)
            {
                case TokenExchangeOutcome.Success success:
                    return new PasswordSignInOutcome.Success(CreateAuthenticationSuccess(success.Response));
                case TokenExchangeOutcome.DeviceVerificationRequired dv:
                    return new PasswordSignInOutcome.DeviceVerificationRequired(dv.Message);
                case TokenExchangeOutcome.InvalidCredentials ic:
                    return new PasswordSignInOutcome.InvalidCredentials(ic.Message);

                case TokenExchangeOutcome.TwoFactorRequired twoFactorRequired:
                    currentContinuation = null;
                    return new PasswordSignInOutcome.TwoFactorRequired(twoFactorRequired.Challenge, signInContinuation);

                default:
                    throw new InvalidOperationException("Unsupported password token outcome.");
            }
        }
        finally
        {
            currentContinuation?.Dispose();
        }
    }

    private static AuthenticationSuccess CreateAuthenticationSuccess(TokenResponseModel model)
    {
        var jwt = JwtSecurityTokenHandler.ReadJwtToken(model.AccessToken.Value);
        string accountId = jwt.Claims.First(c => c.Type == "sub").Value;
        string email = jwt.Claims.First(c => c.Type == "email").Value;

        return new AuthenticationSuccess(UserId.Parse(accountId), email,
            new SessionTokens(model.AccessToken, model.RefreshToken, model.TwoFactorToken, model.ExpiresAt),
            new AccountUnlockData(model.MasterPasswordUnlockModel.KdfConfig, model.MasterPasswordUnlockModel.UserKey, model.MasterPasswordUnlockModel.Salt));
    }
}