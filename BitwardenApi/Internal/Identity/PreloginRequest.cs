using System.Text.Json.Serialization;

namespace BitwardenApi.Internal.Identity;

internal sealed class PreloginRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }
}
