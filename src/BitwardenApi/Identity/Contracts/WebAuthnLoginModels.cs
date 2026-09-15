using System.Text.Json.Serialization;
using BitwardenApi.Infrastructure.Encoding;

namespace BitwardenApi.Identity.Contracts;

public sealed record WebAuthnLoginAssertionOptionsResult(
    WebAuthnLoginAssertionOptions Options,
    WebAuthnLoginAssertionOptionsToken Token);

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

public sealed class WebAuthnPublicKeyCredentialDescriptor
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("id")]
    [JsonConverter(typeof(Base64UrlByteArrayJsonConverter))]
    public required byte[] Id { get; init; }

    [JsonPropertyName("transports")]
    public string[]? Transports { get; init; }
}

public sealed record WebAuthnLoginRequest(
    BitwardenClientContext Context,
    WebAuthnLoginAssertionOptionsToken Token,
    WebAuthnLoginAssertionResponseRequest DeviceResponse,
    string Scope = "api offline_access");

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
