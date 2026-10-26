using System.Text.Json.Serialization;




using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Serialization;
namespace BitwardenApi.Vault.Sync.Responses;
public sealed class VaultOrganizationResponse
{
    public required OrganizationId Id { get; init; }
    public required UserId UserId { get; init; }
    public required Guid OrganizationUserId { get; init; }
    public required string Name { get; init; }
    public int Status { get; init; }

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public bool Enabled { get; init; }

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public bool AccessSecretsManager { get; init; }

    [JsonPropertyName("key")]
    public AsymmetricEncString ProtectedOrganizationKey { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Settings { get; set; } = [];
}
