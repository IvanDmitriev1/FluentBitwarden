using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Utilities;

namespace BitwaredApi.Http;

public sealed class IdentityClient(HttpClient httpClient, IClock clock, IEnvironmentConfig environmentConfig)
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
            int expiresIn = root.TryGetProperty("expires_in", out JsonElement expiresProp)
                ? expiresProp.GetInt32()
                : 900;

            KdfConfigModel? kdf = null;
            if (root.TryGetProperty("kdf", out JsonElement kdfTypeProp) && kdfTypeProp.ValueKind == JsonValueKind.Number)
            {
                kdf = new KdfConfigModel(
                    (KdfType)kdfTypeProp.GetInt32(),
                    root.TryGetProperty("kdfIterations", out JsonElement iterProp) ? iterProp.GetInt32() : 0,
                    root.TryGetProperty("kdfMemory", out JsonElement memProp) && memProp.ValueKind != JsonValueKind.Null ? memProp.GetInt32() : null,
                    root.TryGetProperty("kdfParallelism", out JsonElement parProp) && parProp.ValueKind != JsonValueKind.Null ? parProp.GetInt32() : null);
            }

            UserDecryptionOptionsModel? decryptionOptions = null;
            if (root.TryGetProperty("userDecryptionOptions", out JsonElement userDec) && userDec.ValueKind == JsonValueKind.Object)
            {
                MasterPasswordUnlockModel? unlock = null;
                if (userDec.TryGetProperty("masterPasswordUnlock", out JsonElement mpUnlock) && mpUnlock.ValueKind == JsonValueKind.Object)
                {
                    JsonElement kdfElement = mpUnlock.GetProperty("kdf");
                    unlock = new MasterPasswordUnlockModel(
                        mpUnlock.GetProperty("salt").GetString() ?? string.Empty,
                        new KdfConfigModel(
                            (KdfType)kdfElement.GetProperty("kdfType").GetInt32(),
                            kdfElement.GetProperty("iterations").GetInt32(),
                            kdfElement.TryGetProperty("memory", out JsonElement memory) && memory.ValueKind != JsonValueKind.Null ? memory.GetInt32() : null,
                            kdfElement.TryGetProperty("parallelism", out JsonElement parallelism) && parallelism.ValueKind != JsonValueKind.Null ? parallelism.GetInt32() : null),
                        mpUnlock.GetProperty("masterKeyEncryptedUserKey").GetString() ?? string.Empty);
                }

                decryptionOptions = new UserDecryptionOptionsModel(
                    userDec.TryGetProperty("hasMasterPassword", out JsonElement hasMp) && hasMp.GetBoolean(),
                    unlock);
            }

            return new TokenResponseModel(
                root.GetProperty("access_token").GetString() ?? throw new ServerVersionMismatchException("Identity token response did not include access_token."),
                root.GetProperty("token_type").GetString() ?? "Bearer",
                clock.UtcNow.AddSeconds(expiresIn),
                root.TryGetProperty("refresh_token", out JsonElement refreshProp) ? refreshProp.GetString() : null,
                root.TryGetProperty("key", out JsonElement keyProp) ? keyProp.GetString() : null,
                root.TryGetProperty("privateKey", out JsonElement privateKeyProp) ? privateKeyProp.GetString() : null,
                root.TryGetProperty("twoFactorToken", out JsonElement twoFactorTokenProp) ? twoFactorTokenProp.GetString() : null,
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

    private Uri BuildUri(string relativePath)
        => new(environmentConfig.Current.IdentityBase, relativePath);

    internal sealed class TokenEndpointException(TwoFactorChallenge challenge) : Exception
    {
        public TwoFactorChallenge Challenge { get; } = challenge;
    }
}
