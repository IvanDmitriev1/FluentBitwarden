namespace BitwaredApi.Models.Vault;

public sealed record SyncSummary(
    int CipherCount,
    int FolderCount,
    int CollectionCount,
    DateTimeOffset? RevisionDate,
    DateTimeOffset LastSyncUtc);
