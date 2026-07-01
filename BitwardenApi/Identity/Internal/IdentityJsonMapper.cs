using System.Globalization;

namespace BitwardenApi.Identity.Internal;

internal static class IdentityJsonMapper
{
    public static TokenRefreshSessionModel ToTokenRefreshSessionModel(this IdentityTokenRefreshSessionResponse dto)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(dto.ExpiresInSeconds);
        return new TokenRefreshSessionModel(dto.AccessToken, dto.RefreshToken, dto.TwoFactorToken, expiresAt);
    }

    public static TokenAuthenticatedModel ToTokenResponse(this IdentityTokenAuthenticatedResponse dto)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(dto.ExpiresInSeconds);

        return new TokenAuthenticatedModel(
            dto.AccessToken,
            dto.RefreshToken,
            dto.TwoFactorToken,
            expiresAt,
            EncryptedPrivateKey.Create(dto.EncryptedPrivateKey),
            dto.UserDecryptionOptions.MasterPasswordUnlock.ToMasterPasswordUnlockModel());
    }

    public static TokenExchangeOutcome ToTokenFailureOutcome(this IdentityTokenFailureResponse dto)
    {
        if (dto.DeviceVerificationRequest == true)
        {
            return new TokenExchangeOutcome.DeviceVerificationRequired(dto.ErrorDescription);
        }

        return new TokenExchangeOutcome.TwoFactorRequired(new IdentityTwoFactorChallenge(dto.TwoFactorProviders2), dto.ErrorDescription);
    }

    private static MasterPasswordUnlockModel ToMasterPasswordUnlockModel(this MasterPasswordUnlock dto)
    {
        var masterPasswordKdf = dto.Kdf;

        KdfConfig kdfConfig = masterPasswordKdf.KdfType switch
        {
            KdfType.Pbkdf2Sha256 => new KdfConfig.Pbkdf2(masterPasswordKdf.Iterations),
            KdfType.Argon2Id => new KdfConfig.Argon2Id(masterPasswordKdf.Iterations, masterPasswordKdf.Memory!.Value,
                masterPasswordKdf.Parallelism!.Value),
            _ => throw new ArgumentOutOfRangeException()
        };

        return new MasterPasswordUnlockModel(
            kdfConfig,
            dto.Salt,
            EncryptedUserKey.Create(dto.MasterKeyEncryptedUserKey));
    }
}

