using BitwaredApi.Models.Vault;

namespace BitwaredApi.Abstractions;

public interface IVaultSyncWriter
{
    ValueTask<IVaultSyncWriteSession> BeginReplaceAsync(
        VaultAccountRecord account,
        CancellationToken cancellationToken = default);
}

public interface IVaultSyncWriteSession : IAsyncDisposable
{
    ValueTask WriteCipherAsync(
        CipherSyncItem item,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);

    ValueTask WriteFolderAsync(
        FolderSyncItem item,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);

    ValueTask WriteCollectionAsync(
        CollectionSyncItem item,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);

    ValueTask CommitAsync(
        VaultSyncStateRecord syncState,
        CancellationToken cancellationToken = default);
}
