namespace BitwaredApi.Models.Vault;

using BitwaredApi;

public sealed record VaultSyncRequest(
    BitwardenEnvironment Environment,
    string AccessToken,
    string AccountId,
    string Email,
    bool HasCachedData,
    DateTimeOffset? CachedRevisionDate,
    DateTimeOffset? LastSyncUtc,
    int CachedCipherCount,
    int CachedFolderCount,
    int CachedCollectionCount);

public abstract record VaultSyncResult
{
    private VaultSyncResult()
    {
    }

    public sealed record NotModified(SyncSummary Summary) : VaultSyncResult;

    public sealed record Updated(SyncSummary Summary) : VaultSyncResult;
}

public sealed record SyncSummary(
    int CipherCount,
    int FolderCount,
    int CollectionCount,
    DateTimeOffset? RevisionDate,
    DateTimeOffset LastSyncUtc);
