using System.Text.Json.Serialization;

using BitwardenApi.Identity.Models;
namespace BitwardenApi.Identity.Internal;

internal sealed class WebAuthnAuthenticationAssertionOptionsResponse
{
    [JsonPropertyName("options")]
    public required WebAuthnLoginAssertionOptions Options { get; init; }

    [JsonPropertyName("token")]
    public required WebAuthnLoginAssertionOptionsToken Token { get; init; }

    [JsonPropertyName("object")]
    public string? Object { get; init; }
}

