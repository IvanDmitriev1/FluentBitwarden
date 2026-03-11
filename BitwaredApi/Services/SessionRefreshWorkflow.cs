using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;

namespace BitwaredApi.Services;

internal sealed class SessionRefreshWorkflow(IIdentityClient identityClient) : ISessionRefreshWorkflow
{
    public async ValueTask<SessionRefreshOutcome> RefreshAsync(
        SessionRefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        RefreshTokenRequestModel tokenRequest = new(
            request.Session.RefreshToken,
            ClientType.Desktop,
            request.DeviceInfo.DeviceType,
            request.DeviceInfo.DeviceName,
            request.DeviceInfo.DeviceIdentifier);

        TokenExchangeOutcome refreshOutcome = await identityClient.RefreshTokenAsync(
            request.Session.Environment,
            tokenRequest,
            cancellationToken);

        if (refreshOutcome is not TokenExchangeOutcome.Success success)
        {
            return refreshOutcome switch
            {
                TokenExchangeOutcome.InvalidCredentials invalidCredentials => new SessionRefreshOutcome.ReauthenticationRequired(
                    invalidCredentials.Message),
                TokenExchangeOutcome.DeviceVerificationRequired deviceVerificationRequired => new SessionRefreshOutcome.ReauthenticationRequired(
                    deviceVerificationRequired.Message),
                TokenExchangeOutcome.TwoFactorRequired twoFactorRequired => new SessionRefreshOutcome.ReauthenticationRequired(
                    twoFactorRequired.Message),
                _ => throw new ServerVersionMismatchException("The token endpoint returned an unsupported refresh outcome."),
            };
        }

        TokenResponseModel token = success.Response;
        PersistableSession updatedSession = request.Session with
        {
            RefreshToken = token.RefreshToken ?? request.Session.RefreshToken,
            AccessTokenExpiresAt = token.ExpiresAt,
            MasterKeyEncryptedUserKey = token.UserDecryptionOptions?.MasterPasswordUnlock?.MasterKeyEncryptedUserKey
                ?? token.Key
                ?? request.Session.MasterKeyEncryptedUserKey,
            PrivateKey = token.PrivateKey ?? request.Session.PrivateKey,
            MasterPasswordSalt = token.UserDecryptionOptions?.MasterPasswordUnlock?.Salt ?? request.Session.MasterPasswordSalt,
            KdfConfig = token.UserDecryptionOptions?.MasterPasswordUnlock?.Kdf
                ?? token.Kdf
                ?? request.Session.KdfConfig,
            DeviceIdentifier = request.DeviceInfo.DeviceIdentifier,
        };

        return new SessionRefreshOutcome.Success(
            updatedSession,
            token.AccessToken);
    }
}
