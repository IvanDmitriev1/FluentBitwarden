using BitwardenApi.Vault.Serialization;
using System.Text.Json.Serialization;

namespace BitwardenApi.Models;

public sealed record VaultSyncResponse(
    [property: JsonPropertyName("folders")] List<VaultFolderDto> Folders,
    [property: JsonPropertyName("collections")] List<VaultCollectionDto> Collections,
    [property: JsonPropertyName("ciphers")] List<VaultCipherDto> VaultCiphers);

public struct VaultFolderDto
{
    public FolderId Id { get; set; }
    public DateTimeOffset RevisionDate { get; set; }

    [JsonPropertyName("name")]
    public EncString EncryptedName { get; set; }
}

public record struct VaultCollectionDto
{
    public CollectionId Id { get; set; }
    public OrganizationId? OrganizationId { get; set; }
    public bool ReadOnly { get; set; }
    public bool Manage { get; set; }
    public bool HidePasswords { get; set; }
    public int? Type { get; set; }

    [JsonPropertyName("name")]
    public EncString EncryptedName { get; set; }
}

public readonly struct VaultCipherDto
{
    public required CipherId Id { get; init; }
    public OrganizationId? OrganizationId { get; init; }
    public FolderId? FolderId { get; init; }

    [JsonPropertyName("key")]
    public EncString? EncryptedKey { get; init; }

    [JsonPropertyName("type")]
    public required CipherType CipherType { get; init; }

    public required DateTimeOffset RevisionDate { get; init; }
    public required DateTimeOffset CreationDate { get; init; }
    public DateTimeOffset? DeletedDate { get; init; }
    public DateTimeOffset? ArchivedDate { get; init; }

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public required bool Favorite { get; init; }

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public required bool Reprompt { get; init; }

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public required bool Edit { get; init; }

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public required bool ViewPassword { get; init; }

    [JsonPropertyName("data")]
    [JsonConverter(typeof(StringToUtf8BytesConverter))]
    public required byte[] Data { get; init; }
}

