using System.Text.Json.Serialization;

using BitwardenApi.Serialization;
namespace BitwardenApi.Identity.Models;


public sealed class WebAuthnLoginAssertionOptions
{
    [JsonPropertyName("challenge")]
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] Challenge { get; init; }

    [JsonPropertyName("timeout")]
    public uint TimeoutMilliseconds { get; init; }

    [JsonPropertyName("rpId")]
    public required string RpId { get; init; }
}

