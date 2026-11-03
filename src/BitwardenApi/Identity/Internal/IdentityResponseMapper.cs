using BitwardenApi.Identity.Models;
using BitwardenApi.Cryptography.KeyDerivation;
using BitwardenApi.Cryptography.Keys;

namespace BitwardenApi.Identity.Internal;

internal static class IdentityResponseMapper
{
    public static PasswordPreloginData ToPasswordPreloginData(this PreloginResponse dto)
    {
        KdfType kdfType;
        int iterations;
        int? memory;
        int? parallelism;

        if (dto.KdfSettings is { } settings)
        {
            kdfType = settings.KdfType;
            iterations = settings.Iterations;
            memory = settings.Memory;
            parallelism = settings.Parallelism;
        }
        else
        {
            if (dto.Kdf is not { } legacyKdf)
                throw new InvalidDataException("Prelogin response is missing KDF settings.");

            kdfType = (KdfType)legacyKdf;
            iterations = dto.KdfIterations ?? 0;
            memory = dto.KdfMemory;
            parallelism = dto.KdfParallelism;
        }

        if (iterations <= 0)
            throw new InvalidDataException("Prelogin response has invalid KDF iterations.");

        KdfConfig kdfConfig = kdfType switch
        {
            KdfType.Pbkdf2Sha256 => new KdfConfig.Pbkdf2(iterations),
            KdfType.Argon2Id => CreateArgon2Config(iterations, memory, parallelism),
            _ => throw new InvalidDataException($"Prelogin response has unsupported KDF type '{(int)kdfType}'.")
        };

        return new PasswordPreloginData(kdfConfig, dto.Salt);
    }

    private static KdfConfig.Argon2Id CreateArgon2Config(
        int iterations,
        int? memory,
        int? parallelism)
    {
        int memoryMib = memory.GetValueOrDefault();
        int parallelismValue = parallelism.GetValueOrDefault();
        if (memoryMib <= 0 || parallelismValue <= 0)
            throw new InvalidDataException("Prelogin response has invalid Argon2id settings.");

        if (memoryMib > int.MaxValue / 1024)
            throw new InvalidDataException("Prelogin response Argon2id memory setting is too large.");

        return new KdfConfig.Argon2Id(iterations, memoryMib, parallelismValue);
    }

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

