using System.Text.Json.Serialization;

namespace BitwardenApi.Infrastructure.Identity.Payloads;

internal sealed class PreloginRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }
}
