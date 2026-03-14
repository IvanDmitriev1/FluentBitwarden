using BitwaredApi.Models.Vault;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.Storage;

namespace FluentBitwarden.Services.Storage;

internal sealed class SqliteVaultCache(
    IVaultCipherReadStore cipherReadStore,
    IVaultSyncStateStore syncStateStore,
    IVaultAccountClearStore clearStore)
    : IVaultCache
{
    public ValueTask VisitCiphersAsync(
        string accountId,
        Func<CipherSyncItem, Stream, CancellationToken, ValueTask<bool>> visitAsync,
        CancellationToken cancellationToken = default)
        => cipherReadStore.VisitByAccountAsync(accountId, visitAsync, cancellationToken);

    public ValueTask<bool> VisitCipherAsync(
        string accountId,
        string id,
        Func<CipherSyncItem, Stream, CancellationToken, ValueTask> visitAsync,
        CancellationToken cancellationToken = default)
        => cipherReadStore.VisitByIdAsync(accountId, id, visitAsync, cancellationToken);

    public ValueTask<VaultSyncStateRecord?> GetSyncStateAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => syncStateStore.GetByAccountAsync(accountId, cancellationToken);

    public ValueTask ClearAccountAsync(string accountId, CancellationToken cancellationToken = default)
        => clearStore.ClearAccountAsync(accountId, cancellationToken);
}
