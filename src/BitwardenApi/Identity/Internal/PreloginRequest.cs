using System.Text.Json.Serialization;

namespace BitwardenApi.Identity.Internal;

internal sealed class PreloginRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

