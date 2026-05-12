using System.Text.Json.Serialization;
using BitwardenApi.Models;

namespace BitwardenApi.Infrastructure.Identity.Payloads;

internal sealed class WebAuthnLoginAssertionOptionsResponse
{
    [JsonPropertyName("options")]
    public required WebAuthnLoginAssertionOptions Options { get; init; }

    [JsonPropertyName("token")]
    public required WebAuthnLoginAssertionOptionsToken Token { get; init; }

    [JsonPropertyName("object")]
    public string? Object { get; init; }
}
