using BitwaredApi.Models.Vault;

namespace FluentBitwarden.Abstractions.Storage;

internal interface IVaultCipherReadStore
{
    ValueTask VisitByAccountAsync(
        string accountId,
        Func<CipherSyncItem, Stream, CancellationToken, ValueTask<bool>> visitAsync,
        CancellationToken cancellationToken = default);

    ValueTask<bool> VisitByIdAsync(
        string accountId,
        string id,
        Func<CipherSyncItem, Stream, CancellationToken, ValueTask> visitAsync,
        CancellationToken cancellationToken = default);
}
