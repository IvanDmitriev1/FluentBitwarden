using BitwaredApi.Models.Vault;

namespace FluentBitwarden.Abstractions.Storage;

internal interface IVaultSyncStateStore
{
    ValueTask<VaultSyncStateRecord?> GetByAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
