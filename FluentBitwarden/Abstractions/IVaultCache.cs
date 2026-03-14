using BitwaredApi.Models.Vault;

namespace FluentBitwarden.Abstractions;

/// <summary>
/// Stores and retrieves the encrypted local vault cache for offline access.
/// </summary>
internal interface IVaultCache
{
    ValueTask VisitCiphersAsync(
        string accountId,
        Func<CipherSyncItem, Stream, CancellationToken, ValueTask<bool>> visitAsync,
        CancellationToken cancellationToken = default);

    ValueTask<bool> VisitCipherAsync(
        string accountId,
        string id,
        Func<CipherSyncItem, Stream, CancellationToken, ValueTask> visitAsync,
        CancellationToken cancellationToken = default);

    ValueTask<VaultSyncStateRecord?> GetSyncStateAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    ValueTask ClearAccountAsync(string accountId, CancellationToken cancellationToken = default);
}
