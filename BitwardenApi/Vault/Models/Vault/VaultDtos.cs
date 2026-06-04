using BitwardenApi.Vault.Serialization;
using System.Text.Json.Serialization;

namespace BitwardenApi.Models;

public sealed record VaultSyncResponse
{
    [JsonPropertyName("profile")]
    public required VaultProfileDto Profile { get; init; }

    [JsonPropertyName("folders")]
    public VaultFolderDto[] Folders { get; init; } = [];

    [JsonPropertyName("collections")]
    public VaultCollectionDto[] Collections { get; init; } = [];

    [JsonPropertyName("ciphers")]
    public VaultCipherDto[] VaultCiphers { get; init; } = [];
}

public sealed record VaultProfileDto
{
    public VaultOrganizationDto[]? Organizations { get; init; }
}

public readonly record struct VaultOrganizationDto()
{
    public required OrganizationId Id { get; init; } = OrganizationId.Empty;
    public Guid? OrganizationUserId { get; init; }
    public required string Name { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public bool UseKeyConnector { get; init; }
    public int? Status { get; init; }

    [JsonPropertyName("type")]
    public int? MemberType { get; init; }

    [JsonPropertyName("key")]
    public EncString EncryptedOrganizationKey { get; init; } = EncString.Empty;
}

public struct VaultFolderDto
{
    public FolderId Id { get; set; }
    public DateTimeOffset RevisionDate { get; set; }

    [JsonPropertyName("name")]
    public EncString EncryptedName { get; set; }
}

public record struct VaultCollectionDto()
{
    public CollectionId Id { get; set; } = CollectionId.Empty;

    [JsonConverter(typeof(OptionalOrganizationIdJsonConverter))]
    public OrganizationId OrganizationId { get; set; } = OrganizationId.Empty;

    public bool ReadOnly { get; set; }
    public bool Manage { get; set; }
    public bool HidePasswords { get; set; }
    public int? Type { get; set; }

    [JsonPropertyName("name")]
    public EncString EncryptedName { get; set; } = EncString.Empty;
}

public readonly struct VaultCipherDto()
{
    public required CipherId Id { get; init; } = CipherId.Empty;

    [JsonConverter(typeof(OptionalOrganizationIdJsonConverter))]
    public OrganizationId OrganizationId { get; init; } = OrganizationId.Empty;

    [JsonConverter(typeof(OptionalFolderIdJsonConverter))]
    public FolderId FolderId { get; init; } = FolderId.Empty;

    [JsonPropertyName("collectionIds")]
    [JsonConverter(typeof(CollectionIdsJsonConverter))]
    public CollectionId[] CollectionIds
    {
        get => field ?? [];
        init;
    } = [];

    /// <summary>
    /// More recent ciphers uses individual encryption keys to encrypt the other fields of the Cipher.
    /// </summary>
    [JsonPropertyName("key")]
    public EncString EncryptedKey { get; init; } = EncString.Empty;

    [JsonPropertyName("type")]
    public required CipherType CipherType { get; init; } = default;

    public required DateTimeOffset RevisionDate { get; init; } = default;
    public required DateTimeOffset CreationDate { get; init; } = default;
    public DateTimeOffset? DeletedDate { get; init; }
    public DateTimeOffset? ArchivedDate { get; init; }

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public required bool Favorite { get; init; } = false;

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public required bool Reprompt { get; init; } = false;

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public required bool Edit { get; init; } = false;

    [JsonConverter(typeof(BooleanOrNumberJsonConverter))]
    public required bool ViewPassword { get; init; } = false;

    [JsonPropertyName("data")]
    [JsonConverter(typeof(StringToUtf8BytesConverter))]
    public required byte[] Data { get; init; } = [];
}

