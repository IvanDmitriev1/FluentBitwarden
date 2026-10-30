using System.Text.Json.Serialization;

using BitwardenApi.Serialization;
namespace BitwardenApi.Identity.Models;


internal sealed class WebAuthnPublicKeyCredentialDescriptor
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("id")]
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] Id { get; init; }

    [JsonPropertyName("transports")]
    public string[]? Transports { get; init; }
}

