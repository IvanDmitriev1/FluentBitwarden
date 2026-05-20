using System.Text.Json.Serialization;

namespace BitwardenApi.Identity.Infrastructure.Payloads;

internal sealed class PreloginRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

