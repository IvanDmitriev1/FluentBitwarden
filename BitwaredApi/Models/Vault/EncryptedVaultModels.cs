namespace BitwaredApi.Models.Vault;

public sealed record VaultAccountRecord(
    string AccountId,
    string Email,
    string ApiBase,
    string IdentityBase,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? LastSyncUtc);

public sealed record VaultSyncStateRecord(
    string AccountId,
    DateTimeOffset? RevisionDate,
    DateTimeOffset LastSyncUtc);

public sealed record EncryptedCipherRecord(
    string AccountId,
    string Id,
    int Type,
    string? OrganizationId,
    string? FolderId,
    string CollectionIdsJson,
    DateTimeOffset? RevisionDate,
    string EncryptedJson,
    DateTimeOffset UpdatedUtc);

public sealed record EncryptedFolderRecord(
    string AccountId,
    string Id,
    DateTimeOffset? RevisionDate,
    string EncryptedJson,
    DateTimeOffset UpdatedUtc);

public sealed record EncryptedCollectionRecord(
    string AccountId,
    string Id,
    DateTimeOffset? RevisionDate,
    string EncryptedJson,
    DateTimeOffset UpdatedUtc);

public sealed record EncryptedSyncSnapshot(
    VaultAccountRecord Account,
    VaultSyncStateRecord SyncState,
    IReadOnlyList<EncryptedCipherRecord> Ciphers,
    IReadOnlyList<EncryptedFolderRecord> Folders,
    IReadOnlyList<EncryptedCollectionRecord> Collections);
