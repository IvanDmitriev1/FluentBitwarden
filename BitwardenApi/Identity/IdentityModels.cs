using System.Text.Json;
using System.Text.Json.Serialization;
using BitwardenApi.Primitives;

namespace BitwardenApi.Identity;

public enum TwoFactorProviderType
{
    Authenticator = 0,
    Email = 1,
    Duo = 2,
    YubiKey = 3,
    U2f = 4,
    Remember = 5,
    OrganizationDuo = 6,
    Fido2WebAuthn = 7,
}

public sealed record TwoFactorProof(
    string Code,
    TwoFactorProviderType Provider);

public sealed record TokenResponse
{
    [JsonPropertyName("access_token")]
    public AccessToken AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresInSeconds { get; init; }

    [JsonPropertyName("refresh_token")]
    public RefreshToken? RefreshToken { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extra { get; init; } = [];
}
