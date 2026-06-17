using System.Globalization;

namespace BitwardenApi.Identity.Internal;

internal static class IdentityJsonMapper
{
    public static TokenRefreshSessionModel ToTokenRefreshSessionModel(this TokenRefreshSessionResponse dto)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(dto.ExpiresInSeconds);
        return new TokenRefreshSessionModel(dto.AccessToken, dto.RefreshToken, dto.TwoFactorToken, expiresAt);
    }

    public static TokenAuthenticatedModel ToTokenResponse(this TokenAuthenticatedResponse dto)
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

    public static TokenExchangeOutcome ToTokenFailureOutcome(this TokenFailureResponse dto)
    {
        if (dto.DeviceVerificationRequest == true)
        {
            return new TokenExchangeOutcome.DeviceVerificationRequired(dto.ErrorDescription);
        }

        if (dto.TwoFactorProviders2 is not { Count: > 0 } providers)
            return new TokenExchangeOutcome.InvalidCredentials(dto.ErrorDescription);

        var providerOptions = new List<TwoFactorProviderOption>(dto.TwoFactorProviders2.Count);

        foreach (var (providerKey, metadata) in providers)
        {
            if (!int.TryParse(providerKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedProvider))
            {
                throw new InvalidDataException(
                    $"Token response included an invalid two-factor provider id '{providerKey}'.");
            }

            providerOptions.Add(new TwoFactorProviderOption(
                (TwoFactorProviderType)parsedProvider,
                metadata ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal)));
        }

        return new TokenExchangeOutcome.TwoFactorRequired(
            new TwoFactorChallenge(providerOptions),
            dto.ErrorDescription);

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

