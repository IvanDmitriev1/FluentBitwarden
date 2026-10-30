using System.Text.Json.Serialization;

using BitwardenApi.Serialization;
namespace BitwardenApi.Identity.Models;


public sealed class WebAuthnLoginAssertionResponseData
{
    [JsonPropertyName("authenticatorData")]
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] AuthenticatorData { get; init; }

    [JsonPropertyName("signature")]
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] Signature { get; init; }

    [JsonPropertyName("clientDataJSON")]
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] ClientDataJson { get; init; }

    [JsonPropertyName("userHandle")]
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] UserHandle { get; init; }
}
