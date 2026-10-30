using BitwardenApi.Identity.Models;
using BitwardenApi.Cryptography.KeyDerivation;
using BitwardenApi.Cryptography.Keys;

namespace BitwardenApi.Identity.Internal;

internal static class IdentityResponseMapper
{
    public static RefreshedSession ToTokenRefreshSessionModel(this IdentityTokenRefreshSessionResponse dto)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(dto.ExpiresInSeconds);
        return new RefreshedSession(dto.SessionAccessToken, dto.SessionRefreshToken, dto.TwoFactorToken, expiresAt);
    }

    public static AuthenticatedSession ToTokenResponse(this IdentityTokenAuthenticatedResponse dto)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(dto.ExpiresInSeconds);

        return new AuthenticatedSession(
            dto.SessionAccessToken,
            dto.SessionRefreshToken,
            dto.TwoFactorToken,
            expiresAt,
            ProtectedPrivateKey.Create(dto.ProtectedPrivateKey),
            dto.UserDecryptionOptions.MasterPasswordUnlock.ToMasterPasswordUnlockModel());
    }

    public static SessionTokenRejection ToTokenRejection(this IdentityTokenFailureResponse dto)
    {
        if (dto.DeviceVerificationRequest == true)
        {
            return new SessionTokenRejection(SessionTokenRejectionKind.DeviceVerificationRequired, dto.ErrorDescription);
        }

        if (dto.TwoFactorProviders2 is { Count: > 0 } providers)
        {
            return new SessionTokenRejection(
                SessionTokenRejectionKind.TwoFactorRequired,
                dto.ErrorDescription,
                new IdentityTwoFactorChallenge(providers));
        }

        return new SessionTokenRejection(SessionTokenRejectionKind.InvalidCredentials, dto.ErrorDescription);
    }

    private static MasterPasswordUnlockData ToMasterPasswordUnlockModel(this MasterPasswordUnlock dto)
    {
        var masterPasswordKdf = dto.Kdf;

        KdfConfig kdfConfig = masterPasswordKdf.KdfType switch
        {
            KdfType.Pbkdf2Sha256 => new KdfConfig.Pbkdf2(masterPasswordKdf.Iterations),
            KdfType.Argon2Id => new KdfConfig.Argon2Id(masterPasswordKdf.Iterations, masterPasswordKdf.Memory!.Value,
                masterPasswordKdf.Parallelism!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(dto), masterPasswordKdf.KdfType, "Unsupported KDF type.")
        };

        return new MasterPasswordUnlockData(
            kdfConfig,
            dto.Salt,
            ProtectedUserKey.Create(dto.MasterKeyEncryptedUserKey));
    }
}

