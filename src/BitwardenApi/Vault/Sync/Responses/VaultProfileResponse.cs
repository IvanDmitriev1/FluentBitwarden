using System.Text.Json.Serialization;




namespace BitwardenApi.Vault.Sync.Responses;
public sealed class VaultProfileResponse
{
    public required UserId Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Culture { get; init; }
    public required DateTimeOffset CreationDate { get; init; }

    public required VaultOrganizationResponse[] Organizations { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Settings { get; set; } = [];
}
