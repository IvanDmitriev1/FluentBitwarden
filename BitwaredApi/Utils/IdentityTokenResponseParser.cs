using System.Globalization;
using System.Net;
using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Auth;

namespace BitwaredApi.Utils;

internal static class IdentityTokenResponseParser
{
    public static PreloginResponseModel ParsePreloginResponse(JsonElement root)
    {
        if (root.TryGetProperty("kdfSettings", out JsonElement kdfSettings) && kdfSettings.ValueKind == JsonValueKind.Object)
        {
            return new PreloginResponseModel(
                new KdfConfigModel(
                    (KdfType)kdfSettings.GetRequiredInt32Property(
                        "kdfType",
                        "Prelogin response did not include kdfSettings.kdfType."),
                    kdfSettings.GetRequiredInt32Property(
                        "iterations",
                        "Prelogin response did not include kdfSettings.iterations."),
                    kdfSettings.TryGetProperty("memory", out JsonElement nestedMemory) && nestedMemory.ValueKind != JsonValueKind.Null
                        ? nestedMemory.GetInt32()
                        : null,
                    kdfSettings.TryGetProperty("parallelism", out JsonElement nestedParallelism) && nestedParallelism.ValueKind != JsonValueKind.Null
                        ? nestedParallelism.GetInt32()
                        : null));
        }

        return new PreloginResponseModel(
            new KdfConfigModel(
                (KdfType)root.GetRequiredInt32Property("kdf", "Prelogin response did not include kdf."),
                root.GetRequiredInt32Property("kdfIterations", "Prelogin response did not include kdfIterations."),
                root.TryGetProperty("kdfMemory", out JsonElement memory) && memory.ValueKind != JsonValueKind.Null
                    ? memory.GetInt32()
                    : null,
                root.TryGetProperty("kdfParallelism", out JsonElement parallelism) && parallelism.ValueKind != JsonValueKind.Null
                    ? parallelism.GetInt32()
                    : null));
    }

    public static TokenResponseModel ParseTokenSuccessResponse(JsonElement root, DateTimeOffset issuedAtUtc)
    {
        int expiresIn = root.TryGetAnyFlexibleProperty(out JsonElement expiresProp, "expires_in", "ExpiresIn")
            ? expiresProp.GetInt32()
            : 900;

        KdfConfigModel? kdf = null;
        if (root.TryGetAnyFlexibleProperty(out JsonElement kdfTypeProp, "kdf", "Kdf") && kdfTypeProp.ValueKind == JsonValueKind.Number)
        {
            kdf = new KdfConfigModel(
                (KdfType)kdfTypeProp.GetInt32(),
                root.TryGetAnyFlexibleProperty(out JsonElement iterProp, "kdfIterations", "KdfIterations")
                    ? iterProp.GetInt32()
                    : 0,
                root.TryGetAnyFlexibleProperty(out JsonElement memProp, "kdfMemory", "KdfMemory") && memProp.ValueKind != JsonValueKind.Null
                    ? memProp.GetInt32()
                    : null,
                root.TryGetAnyFlexibleProperty(out JsonElement parProp, "kdfParallelism", "KdfParallelism") && parProp.ValueKind != JsonValueKind.Null
                    ? parProp.GetInt32()
                    : null);
        }

        UserDecryptionOptionsModel? decryptionOptions = null;
        if (root.TryGetAnyFlexibleProperty(out JsonElement userDec, "userDecryptionOptions", "UserDecryptionOptions")
            && userDec.ValueKind == JsonValueKind.Object)
        {
            MasterPasswordUnlockModel? unlock = null;
            if (userDec.TryGetAnyFlexibleProperty(out JsonElement mpUnlock, "masterPasswordUnlock", "MasterPasswordUnlock")
                && mpUnlock.ValueKind == JsonValueKind.Object)
            {
                JsonElement kdfElement = mpUnlock.GetRequiredFlexibleProperty(
                    "Token response did not include required property 'kdf'.",
                    "kdf",
                    "Kdf");

                unlock = new MasterPasswordUnlockModel(
                    mpUnlock.GetRequiredFlexibleString(
                        "Token response did not include required string property 'salt'.",
                        "salt",
                        "Salt"),
                    new KdfConfigModel(
                        (KdfType)kdfElement.GetRequiredFlexibleProperty(
                            "Token response did not include required property 'kdfType'.",
                            "kdfType",
                            "KdfType").GetInt32(),
                        kdfElement.GetRequiredFlexibleProperty(
                            "Token response did not include required property 'iterations'.",
                            "iterations",
                            "Iterations").GetInt32(),
                        kdfElement.TryGetAnyFlexibleProperty(out JsonElement memory, "memory", "Memory")
                            && memory.ValueKind != JsonValueKind.Null
                            ? memory.GetInt32()
                            : null,
                        kdfElement.TryGetAnyFlexibleProperty(out JsonElement parallelism, "parallelism", "Parallelism")
                            && parallelism.ValueKind != JsonValueKind.Null
                            ? parallelism.GetInt32()
                            : null),
                    mpUnlock.GetRequiredFlexibleString(
                        "Token response did not include required string property 'masterKeyEncryptedUserKey'.",
                        "masterKeyEncryptedUserKey",
                        "MasterKeyEncryptedUserKey"));
            }

            decryptionOptions = new UserDecryptionOptionsModel(
                userDec.TryGetAnyFlexibleProperty(out JsonElement hasMasterPassword, "hasMasterPassword", "HasMasterPassword")
                    && hasMasterPassword.GetBoolean(),
                unlock);
        }

        return new TokenResponseModel(
            root.GetRequiredFlexibleProperty(
                    "Identity token response did not include access_token.",
                    "access_token",
                    "AccessToken")
                .GetString()
                ?? throw new ServerVersionMismatchException("Identity token response did not include access_token."),
            root.GetOptionalFlexibleString("token_type", "TokenType") ?? "Bearer",
            issuedAtUtc.AddSeconds(expiresIn),
            root.GetOptionalFlexibleString("refresh_token", "RefreshToken"),
            root.GetOptionalFlexibleString("key", "Key"),
            root.GetOptionalFlexibleString("privateKey", "PrivateKey"),
            root.GetOptionalFlexibleString("twoFactorToken", "TwoFactorToken"),
            kdf,
            decryptionOptions);
    }

    public static async ValueTask<TokenExchangeOutcome> ReadTokenFailureAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(body))
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;

            string? error = root.GetOptionalFlexibleString("error", "Error");
            string? description = root.GetOptionalFlexibleString("error_description", "ErrorDescription");

            if (string.Equals(error, "invalid_grant", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetAnyFlexibleProperty(out JsonElement deviceVerificationProp, "deviceVerificationRequest", "DeviceVerificationRequest")
                    && deviceVerificationProp.ValueKind == JsonValueKind.True)
                {
                    return new TokenExchangeOutcome.DeviceVerificationRequired(
                        description ?? "Device verification required.");
                }

                if (root.TryGetAnyFlexibleProperty(out JsonElement providersProp, "twoFactorProviders2", "TwoFactorProviders2")
                    && providersProp.ValueKind == JsonValueKind.Object)
                {
                    List<TwoFactorProviderOption> providers = [];

                    foreach (JsonProperty provider in providersProp.EnumerateObject())
                    {
                        Dictionary<string, JsonElement> metadata = [];
                        if (provider.Value.ValueKind == JsonValueKind.Object)
                        {
                            foreach (JsonProperty metadataProperty in provider.Value.EnumerateObject())
                            {
                                metadata[metadataProperty.Name] = metadataProperty.Value.Clone();
                            }
                        }

                        providers.Add(new TwoFactorProviderOption(
                            (TwoFactorProviderType)int.Parse(provider.Name, CultureInfo.InvariantCulture),
                            metadata));
                    }

                    return new TokenExchangeOutcome.TwoFactorRequired(
                        new TwoFactorChallenge(
                            providers,
                            true,
                            root.GetOptionalFlexibleString("email", "Email"),
                            root.GetOptionalFlexibleString("ssoEmail2faSessionToken", "SsoEmail2faSessionToken")),
                        description ?? "Two-factor authentication is required to continue.");
                }

                return new TokenExchangeOutcome.InvalidCredentials(
                    description ?? "The supplied credentials were rejected by the server.");
            }
        }

        throw new ServerVersionMismatchException($"Token endpoint returned {(int)response.StatusCode}: {body}");
    }
}
