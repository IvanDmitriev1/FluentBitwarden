using System.Text.Json.Serialization;

using BitwardenApi.Serialization;
namespace BitwardenApi.Identity.Models;


public sealed class WebAuthnLoginAssertionResponseRequest
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("rawId")]
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] RawId { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = "public-key";

    [JsonPropertyName("extensions")]
    public Dictionary<string, string> Extensions { get; init; } = new(StringComparer.Ordinal);

    [JsonPropertyName("response")]
    public required WebAuthnLoginAssertionResponseData Response { get; init; }
}

