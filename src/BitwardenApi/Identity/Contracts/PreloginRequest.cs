using System.Text.Json.Serialization;

namespace BitwardenApi.Identity.Contracts;

internal sealed class PreloginRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

