using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class VaultOperationsIpcHandler(
    IVaultService vaultService) : IVaultOperationsClient, IIpcRequestsHandler
{
    public async Task<VaultSyncResult> SyncAsync(SyncVaultRequest request, CancellationToken cancellationToken = default)
    {
        return await vaultService.SyncAsync(cancellationToken);
    }
}
