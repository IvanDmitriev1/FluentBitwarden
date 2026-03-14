using BitwaredApi.Abstractions;
using BitwaredApi.Models.Vault;

namespace BitwaredApi.Services;

internal sealed class VaultSyncService(IApiClient apiClient) : IVaultSyncService
{
    public async ValueTask<VaultSyncResult> SyncAsync(
        VaultSyncRequest request,
        IVaultSyncWriter writer,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset? remoteRevisionDate = await apiClient.GetRevisionDateAsync(
            request.Environment,
            request.AccessToken,
            cancellationToken).ConfigureAwait(false);

        if (request.HasCachedData
            && remoteRevisionDate is not null
            && request.CachedRevisionDate == remoteRevisionDate)
        {
            return new VaultSyncResult.NotModified(
                new SyncSummary(
                    request.CachedCipherCount,
                    request.CachedFolderCount,
                    request.CachedCollectionCount,
                    request.CachedRevisionDate,
                    request.LastSyncUtc ?? DateTimeOffset.UtcNow));
        }

        using HttpResponseMessage response = await apiClient.CreateSyncResponseAsync(
            request.Environment,
            request.AccessToken,
            cancellationToken).ConfigureAwait(false);

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        DateTimeOffset lastSyncUtc = DateTimeOffset.UtcNow;
        VaultAccountRecord account = new(
            request.AccountId,
            request.Email,
            request.Environment.ApiBase.ToString(),
            request.Environment.IdentityBase.ToString(),
            lastSyncUtc,
            lastSyncUtc);

        await using IVaultSyncWriteSession writeSession = await writer
            .BeginReplaceAsync(account, cancellationToken)
            .ConfigureAwait(false);

        VaultSyncResponseParser.SyncPayloadCounts counts = await VaultSyncResponseParser
            .WriteToStoreAsync(stream, writeSession, cancellationToken)
            .ConfigureAwait(false);

        SyncSummary summary = new(
            counts.CipherCount,
            counts.FolderCount,
            counts.CollectionCount,
            remoteRevisionDate,
            lastSyncUtc);

        await writeSession.CommitAsync(
            new VaultSyncStateRecord(
                request.AccountId,
                remoteRevisionDate,
                lastSyncUtc,
                counts.CipherCount,
                counts.FolderCount,
                counts.CollectionCount),
            cancellationToken).ConfigureAwait(false);

        return new VaultSyncResult.Updated(summary);
    }
}
