using System.Text.Json.Serialization;
using BitwardenApi.Vault.Attachments.Contracts;

namespace BitwardenApi.Vault.Items.Contracts;

public sealed record VaultSyncResponse
{
    [JsonPropertyName("profile")]
    public required VaultProfileResponse Profile { get; init; }

    [JsonPropertyName("folders")]
    public VaultFolderResponse[] Folders { get; init; } = [];

    [JsonPropertyName("collections")]
    public VaultCollectionResponse[] Collections { get; init; } = [];

    [JsonPropertyName("ciphers")]
    public VaultCipherResponse[] VaultCiphers { get; init; } = [];
}

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

public struct VaultFolderResponse
{
    public FolderId Id { get; set; }
    public DateTimeOffset RevisionDate { get; set; }

    [JsonPropertyName("name")]
    public EncString EncryptedName { get; set; }
}

public record struct VaultCollectionResponse
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

public readonly struct VaultCipherResponse
{
    public required CipherId Id { get; init; }

    [JsonConverter(typeof(OptionalOrganizationIdJsonConverter))]
    public OrganizationId OrganizationId { get; init; }

    [JsonConverter(typeof(OptionalFolderIdJsonConverter))]
    public FolderId FolderId { get; init; }

    [JsonPropertyName("collectionIds")]
    [JsonConverter(typeof(CollectionIdsJsonConverter))]
    public CollectionId[] CollectionIds
    {
        get => field ?? [];
        init;
    }

    /// <summary>
    /// More recent ciphers uses individual encryption keys to encrypt the other fields of the Cipher.
    /// </summary>
    [JsonPropertyName("key")]
    public EncString ProtectedCipherKey { get; init; }

    [JsonPropertyName("type")]
    public required VaultCipherType VaultCipherType { get; init; }

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

    public VaultCipherAttachmentDownloadResponse[]? Attachments { get; init; }
}
