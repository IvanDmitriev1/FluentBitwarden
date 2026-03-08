using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Utilities;

namespace BitwaredApi.Services;

internal sealed class IdentityClient(HttpClient httpClient, IClock clock, IEnvironmentConfig environmentConfig) : IIdentityClient
{
    public async ValueTask<PreloginResponseModel> PreloginAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                BuildUri("/accounts/prelogin"),
                new { email },
                JsonDefaults.SerializerOptions,
                cancellationToken).ConfigureAwait(false);

            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            JsonElement root = document.RootElement;
            if (root.TryGetProperty("kdfSettings", out JsonElement kdfSettings) && kdfSettings.ValueKind == JsonValueKind.Object)
            {
                return new PreloginResponseModel(
                    new KdfConfigModel(
                        (KdfType)kdfSettings.GetProperty("kdfType").GetInt32(),
                        kdfSettings.GetProperty("iterations").GetInt32(),
                        kdfSettings.TryGetProperty("memory", out JsonElement nestedMemory) && nestedMemory.ValueKind != JsonValueKind.Null ? nestedMemory.GetInt32() : null,
                        kdfSettings.TryGetProperty("parallelism", out JsonElement nestedParallelism) && nestedParallelism.ValueKind != JsonValueKind.Null ? nestedParallelism.GetInt32() : null));
            }

            return new PreloginResponseModel(
                new KdfConfigModel(
                    (KdfType)root.GetProperty("kdf").GetInt32(),
                    root.GetProperty("kdfIterations").GetInt32(),
                    root.TryGetProperty("kdfMemory", out JsonElement memory) && memory.ValueKind != JsonValueKind.Null ? memory.GetInt32() : null,
                    root.TryGetProperty("kdfParallelism", out JsonElement parallelism) && parallelism.ValueKind != JsonValueKind.Null ? parallelism.GetInt32() : null));
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkUnavailableException(innerException: ex);
        }
    }

    public ValueTask<TokenResponseModel> ExchangePasswordAsync(
        PasswordTokenRequestModel request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(request, cancellationToken);

    public ValueTask<TokenResponseModel> RefreshTokenAsync(
        RefreshTokenRequestModel request,
        CancellationToken cancellationToken = default)
        => SendTokenRequestAsync(request, cancellationToken);

    private async ValueTask<TokenResponseModel> SendTokenRequestAsync(
        TokenRequestModel request,
        CancellationToken cancellationToken)
    {
        using FormUrlEncodedContent form = new(request.ToFormValues());

        try
        {
            using HttpResponseMessage response = await httpClient.PostAsync(
                BuildUri("/connect/token"),
                form,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await ThrowTokenExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }

            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            JsonElement root = document.RootElement;
            int expiresIn = TryGetTokenSuccessProperty(root, out JsonElement expiresProp, "expires_in", "ExpiresIn")
                ? expiresProp.GetInt32()
                : 900;

            KdfConfigModel? kdf = null;
            if (TryGetTokenSuccessProperty(root, out JsonElement kdfTypeProp, "kdf", "Kdf") && kdfTypeProp.ValueKind == JsonValueKind.Number)
            {
                kdf = new KdfConfigModel(
                    (KdfType)kdfTypeProp.GetInt32(),
                    TryGetTokenSuccessProperty(root, out JsonElement iterProp, "kdfIterations", "KdfIterations") ? iterProp.GetInt32() : 0,
                    TryGetTokenSuccessProperty(root, out JsonElement memProp, "kdfMemory", "KdfMemory") && memProp.ValueKind != JsonValueKind.Null ? memProp.GetInt32() : null,
                    TryGetTokenSuccessProperty(root, out JsonElement parProp, "kdfParallelism", "KdfParallelism") && parProp.ValueKind != JsonValueKind.Null ? parProp.GetInt32() : null);
            }

            UserDecryptionOptionsModel? decryptionOptions = null;
            if (TryGetTokenSuccessProperty(root, out JsonElement userDec, "userDecryptionOptions", "UserDecryptionOptions") && userDec.ValueKind == JsonValueKind.Object)
            {
                MasterPasswordUnlockModel? unlock = null;
                if (TryGetTokenSuccessProperty(userDec, out JsonElement mpUnlock, "masterPasswordUnlock", "MasterPasswordUnlock") && mpUnlock.ValueKind == JsonValueKind.Object)
                {
                    JsonElement kdfElement = GetRequiredTokenSuccessProperty(mpUnlock, "kdf", "Kdf");
                    unlock = new MasterPasswordUnlockModel(
                        GetRequiredTokenSuccessString(mpUnlock, "salt", "Salt") ?? string.Empty,
                        new KdfConfigModel(
                            (KdfType)GetRequiredTokenSuccessProperty(kdfElement, "kdfType", "KdfType").GetInt32(),
                            GetRequiredTokenSuccessProperty(kdfElement, "iterations", "Iterations").GetInt32(),
                            TryGetTokenSuccessProperty(kdfElement, out JsonElement memory, "memory", "Memory") && memory.ValueKind != JsonValueKind.Null ? memory.GetInt32() : null,
                            TryGetTokenSuccessProperty(kdfElement, out JsonElement parallelism, "parallelism", "Parallelism") && parallelism.ValueKind != JsonValueKind.Null ? parallelism.GetInt32() : null),
                        GetRequiredTokenSuccessString(mpUnlock, "masterKeyEncryptedUserKey", "MasterKeyEncryptedUserKey") ?? string.Empty);
                }

                decryptionOptions = new UserDecryptionOptionsModel(
                    TryGetTokenSuccessProperty(userDec, out JsonElement hasMp, "hasMasterPassword", "HasMasterPassword") && hasMp.GetBoolean(),
                    unlock);
            }

            return new TokenResponseModel(
                GetRequiredTokenSuccessProperty(root, "access_token", "AccessToken").GetString()
                    ?? throw new ServerVersionMismatchException("Identity token response did not include access_token."),
                GetOptionalTokenSuccessString(root, "token_type", "TokenType") ?? "Bearer",
                clock.UtcNow.AddSeconds(expiresIn),
                GetOptionalTokenSuccessString(root, "refresh_token", "RefreshToken"),
                GetOptionalTokenSuccessString(root, "key", "Key"),
                GetOptionalTokenSuccessString(root, "privateKey", "PrivateKey"),
                GetOptionalTokenSuccessString(root, "twoFactorToken", "TwoFactorToken"),
                kdf,
                decryptionOptions);
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkUnavailableException(innerException: ex);
        }
    }

    private static async ValueTask EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new ServerVersionMismatchException($"Identity endpoint returned {(int)response.StatusCode}: {body}");
    }

    private static async ValueTask ThrowTokenExceptionAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(body))
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;

            string? error = TryGetTokenErrorProperty(root, out JsonElement errorProp, "error", "Error")
                ? errorProp.GetString()
                : null;
            string? description = TryGetTokenErrorProperty(root, out JsonElement descriptionProp, "error_description", "ErrorDescription")
                ? descriptionProp.GetString()
                : null;

            if (string.Equals(error, "invalid_grant", StringComparison.OrdinalIgnoreCase))
            {
                if (TryGetTokenErrorProperty(root, out JsonElement deviceVerificationProp, "deviceVerificationRequest", "DeviceVerificationRequest")
                    && deviceVerificationProp.ValueKind == JsonValueKind.True)
                {
                    throw new InvalidCredentialsException(description ?? "Device verification required.");
                }

                if (TryGetTokenErrorProperty(root, out JsonElement providersProp, "twoFactorProviders2", "TwoFactorProviders2")
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

                        providers.Add(new TwoFactorProviderOption((TwoFactorProviderType)int.Parse(provider.Name),
                            metadata));
                    }

                    throw new TokenEndpointException(
                        error,
                        description,
                        false,
                        new TwoFactorChallenge(
                            providers,
                            true,
                            TryGetTokenErrorProperty(root, out JsonElement emailProp, "email", "Email") ? emailProp.GetString() : null,
                            TryGetTokenErrorProperty(root, out JsonElement sessionTokenProp, "ssoEmail2faSessionToken", "SsoEmail2faSessionToken")
                                ? sessionTokenProp.GetString()
                                : null));
                }

                throw new InvalidCredentialsException(description);
            }
        }

        throw new ServerVersionMismatchException($"Token endpoint returned {(int)response.StatusCode}: {body}");
    }

    private static bool TryGetTokenErrorProperty(JsonElement root, out JsonElement value, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (TryGetTokenErrorProperty(root, propertyName, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetTokenErrorProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        if (root.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        if (string.IsNullOrEmpty(propertyName))
        {
            value = default;
            return false;
        }

        string lowerFirst = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        if (!string.Equals(lowerFirst, propertyName, StringComparison.Ordinal)
            && root.TryGetProperty(lowerFirst, out value))
        {
            return true;
        }

        string lower = propertyName.ToLowerInvariant();
        if (!string.Equals(lower, propertyName, StringComparison.Ordinal)
            && !string.Equals(lower, lowerFirst, StringComparison.Ordinal)
            && root.TryGetProperty(lower, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static JsonElement GetRequiredTokenSuccessProperty(JsonElement root, params string[] propertyNames)
        => TryGetTokenSuccessProperty(root, out JsonElement value, propertyNames)
            ? value
            : throw new ServerVersionMismatchException($"Token response did not include required property '{propertyNames[0]}'.");

    private static string? GetOptionalTokenSuccessString(JsonElement root, params string[] propertyNames)
        => TryGetTokenSuccessProperty(root, out JsonElement value, propertyNames) ? value.GetString() : null;

    private static string GetRequiredTokenSuccessString(JsonElement root, params string[] propertyNames)
        => GetOptionalTokenSuccessString(root, propertyNames)
            ?? throw new ServerVersionMismatchException($"Token response did not include required string property '{propertyNames[0]}'.");

    private static bool TryGetTokenSuccessProperty(JsonElement root, out JsonElement value, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (TryGetTokenSuccessProperty(root, propertyName, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetTokenSuccessProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        if (root.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        if (string.IsNullOrEmpty(propertyName))
        {
            value = default;
            return false;
        }

        string lowerFirst = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        if (!string.Equals(lowerFirst, propertyName, StringComparison.Ordinal)
            && root.TryGetProperty(lowerFirst, out value))
        {
            return true;
        }

        string lower = propertyName.ToLowerInvariant();
        if (!string.Equals(lower, propertyName, StringComparison.Ordinal)
            && !string.Equals(lower, lowerFirst, StringComparison.Ordinal)
            && root.TryGetProperty(lower, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private Uri BuildUri(string relativePath)
        => new(environmentConfig.Current.IdentityBase, relativePath);

}
