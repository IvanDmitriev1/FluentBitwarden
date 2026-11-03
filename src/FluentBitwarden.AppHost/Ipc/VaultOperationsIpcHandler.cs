using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class VaultOperationsIpcHandler(
    IVaultSyncService vaultSyncService) : IVaultOperationsClient, IIpcRequestsHandler
{
    public async Task<VaultSyncResult> SyncAsync(SyncVaultRequest request, CancellationToken cancellationToken = default)
    {
        return await vaultSyncService.SyncAsync();
    }
}
