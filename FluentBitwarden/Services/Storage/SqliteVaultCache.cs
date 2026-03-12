using BitwaredApi.Models.Vault;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.Storage;

namespace FluentBitwarden.Services.Storage;

internal sealed class SqliteVaultCache(
    IVaultCipherReadStore cipherReadStore,
    IVaultSyncStateStore syncStateStore,
    IVaultSnapshotWriteStore snapshotWriteStore)
    : IVaultCache
{
    public ValueTask SaveSyncAsync(EncryptedSyncSnapshot snapshot, CancellationToken cancellationToken = default)
        => snapshotWriteStore.SaveSyncAsync(snapshot, cancellationToken);

    public ValueTask<IReadOnlyList<EncryptedCipherRecord>> ListCiphersAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => cipherReadStore.ListByAccountAsync(accountId, cancellationToken);

    public ValueTask<EncryptedCipherRecord?> GetCipherAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default)
        => cipherReadStore.GetByIdAsync(accountId, id, cancellationToken);

    public ValueTask<VaultSyncStateRecord?> GetSyncStateAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => syncStateStore.GetByAccountAsync(accountId, cancellationToken);

    public ValueTask ClearAccountAsync(string accountId, CancellationToken cancellationToken = default)
        => snapshotWriteStore.ClearAccountAsync(accountId, cancellationToken);
}
