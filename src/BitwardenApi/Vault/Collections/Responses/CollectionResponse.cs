using System.Text.Json.Serialization;




using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Serialization;
using BitwardenApi.Vault.Internal;
namespace BitwardenApi.Vault.Collections.Responses;
public record struct CollectionResponse
{
    public CollectionId Id { get; set; }

    [JsonConverter(typeof(OptionalOrganizationIdJsonConverter))]
    public OrganizationId OrganizationId { get; set; }

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public bool ReadOnly { get; set; }

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public bool Manage { get; set; }

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public bool HidePasswords { get; set; }

    public int Type { get; set; }


    [JsonPropertyName("name")]
    public EncString EncryptedName { get; set; }
}
