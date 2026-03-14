using BitwaredApi.Models.Vault;

namespace BitwaredApi.Abstractions;

public interface IVaultSyncService
{
    ValueTask<VaultSyncResult> SyncAsync(
        VaultSyncRequest request,
        IVaultSyncWriter writer,
        CancellationToken cancellationToken = default);
}
