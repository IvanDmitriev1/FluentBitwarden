using System.Globalization;
using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Serialization;

namespace BitwaredApi.Utils;

internal static class IdentityJsonMapper
{
    public static PreloginResponseModel ToPreloginResponse(PreloginResponseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.KdfSettings is { KdfType: int nestedType, Iterations: int nestedIterations } nested)
        {
            return new PreloginResponseModel(
                new KdfConfigModel(
                    (KdfType)nestedType,
                    nestedIterations,
                    nested.Memory,
                    nested.Parallelism));
        }

        if (dto.Kdf is int type && dto.KdfIterations is int iterations)
        {
            return new PreloginResponseModel(
                new KdfConfigModel(
                    (KdfType)type,
                    iterations,
                    dto.KdfMemory,
                    dto.KdfParallelism));
        }

        throw new ServerVersionMismatchException("Prelogin response did not include KDF settings.");
    }

    public static TokenResponseModel ToTokenResponse(TokenSuccessResponseDto dto, DateTimeOffset issuedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string accessToken = FirstNonEmpty(dto.AccessToken, dto.AccessTokenPascal)
            ?? throw new ServerVersionMismatchException("Identity token response did not include access_token.");

        int expiresIn = FirstNonNull(dto.ExpiresIn, dto.ExpiresInPascal) ?? 900;

        KdfConfigModel? kdf = null;
        if (dto.Kdf is int kdfType)
        {
            kdf = new KdfConfigModel(
                (KdfType)kdfType,
                dto.KdfIterations ?? 0,
                dto.KdfMemory,
                dto.KdfParallelism);
        }

        UserDecryptionOptionsModel? userDecryptionOptions = dto.UserDecryptionOptions is null
            ? null
            : new UserDecryptionOptionsModel(
                dto.UserDecryptionOptions.HasMasterPassword ?? false,
                dto.UserDecryptionOptions.MasterPasswordUnlock is null
                    ? null
                    : new MasterPasswordUnlockModel(
                        dto.UserDecryptionOptions.MasterPasswordUnlock.Salt
                        ?? throw new ServerVersionMismatchException(
                            "Token response did not include required string property 'salt'."),
                        ToRequiredKdfConfig(
                            dto.UserDecryptionOptions.MasterPasswordUnlock.Kdf,
                            "Token response did not include required property 'kdf'."),
                        dto.UserDecryptionOptions.MasterPasswordUnlock.MasterKeyEncryptedUserKey
                        ?? throw new ServerVersionMismatchException(
                            "Token response did not include required string property 'masterKeyEncryptedUserKey'.")));

        return new TokenResponseModel(
            accessToken,
            FirstNonEmpty(dto.TokenType, dto.TokenTypePascal) ?? "Bearer",
            issuedAtUtc.AddSeconds(expiresIn),
            FirstNonEmpty(dto.RefreshToken, dto.RefreshTokenPascal),
            dto.Key,
            dto.PrivateKey,
            dto.TwoFactorToken,
            kdf,
            userDecryptionOptions);
    }

    public static TokenExchangeOutcome ToTokenFailureOutcome(TokenFailureResponseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string? error = dto.Error;
        string? description = FirstNonEmpty(dto.ErrorDescription, dto.ErrorDescriptionPascal);

        if (!string.Equals(error, "invalid_grant", StringComparison.OrdinalIgnoreCase))
        {
            throw new ServerVersionMismatchException("Token endpoint returned an unrecognized error response.");
        }

        if (dto.DeviceVerificationRequest == true)
        {
            return new TokenExchangeOutcome.DeviceVerificationRequired(
                description ?? "Device verification required.");
        }

        if (dto.TwoFactorProviders2 is { Count: > 0 } providers)
        {
            List<TwoFactorProviderOption> providerOptions = [];

            foreach ((string providerKey, Dictionary<string, JsonElement>? metadata) in providers)
            {
                if (!int.TryParse(providerKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedProvider))
                {
                    throw new ServerVersionMismatchException(
                        $"Token response included an invalid two-factor provider id '{providerKey}'.");
                }

                providerOptions.Add(new TwoFactorProviderOption(
                    (TwoFactorProviderType)parsedProvider,
                    metadata ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal)));
            }

            return new TokenExchangeOutcome.TwoFactorRequired(
                new TwoFactorChallenge(
                    providerOptions,
                    true,
                    dto.Email,
                    dto.SsoEmail2FaSessionToken),
                description ?? "Two-factor authentication is required to continue.");
        }

        return new TokenExchangeOutcome.InvalidCredentials(
            description ?? "The supplied credentials were rejected by the server.");
    }

    private static KdfConfigModel ToRequiredKdfConfig(KdfSettingsDto? dto, string errorMessage)
    {
        if (dto is not { KdfType: int type, Iterations: int iterations })
        {
            throw new ServerVersionMismatchException(errorMessage);
        }

        return new KdfConfigModel(
            (KdfType)type,
            iterations,
            dto.Memory,
            dto.Parallelism);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static int? FirstNonNull(params int?[] values)
    {
        foreach (int? value in values)
        {
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }
}
