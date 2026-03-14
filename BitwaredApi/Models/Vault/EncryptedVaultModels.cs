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
    DateTimeOffset LastSyncUtc,
    int CipherCount,
    int FolderCount,
    int CollectionCount);

public sealed record CipherSyncItem(
    string Id,
    int Type,
    string? OrganizationId,
    string? FolderId,
    string CollectionIdsJson,
    DateTimeOffset? RevisionDate);

public sealed record FolderSyncItem(
    string Id,
    DateTimeOffset? RevisionDate);

public sealed record CollectionSyncItem(
    string Id,
    DateTimeOffset? RevisionDate);
