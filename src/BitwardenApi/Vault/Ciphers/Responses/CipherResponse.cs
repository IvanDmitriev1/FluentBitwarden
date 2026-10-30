using System.Text.Json.Serialization;
using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Serialization;
using BitwardenApi.Vault.Attachments.Responses;
using BitwardenApi.Vault.Ciphers.Models;
using BitwardenApi.Vault.Internal;

namespace BitwardenApi.Vault.Ciphers.Responses;

public readonly struct CipherResponse
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
    public required CipherType VaultCipherType { get; init; }

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

    public AttachmentDownloadResponse[]? Attachments { get; init; }
}
