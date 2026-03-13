using BitwaredApi.Models.Vault;

namespace FluentBitwarden.Abstractions.Storage;

internal interface IVaultCipherReadStore
{
    ValueTask<IReadOnlyList<CipherSyncItem>> ListByAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    ValueTask<CipherSyncItem?> GetByIdAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default);
}
