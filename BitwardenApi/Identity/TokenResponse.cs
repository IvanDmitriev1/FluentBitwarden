using System.Text.Json;
using System.Text.Json.Serialization;
using BitwardenApi.Primitives;

namespace BitwardenApi.Identity;

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
