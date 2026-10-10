using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class VaultOperationsIpcHandler(
    IAppSessionService appSessionService,
    IVaultService vaultService) : IVaultOperationsClient, IIpcRequestsHandler
{
    public async Task<VaultSyncResult> SyncAsync(SyncVaultRequest request, CancellationToken cancellationToken = default)
    {
        using var lease = appSessionService.RequireUnlockedSessionLease();

        return await vaultService.SyncAsync(lease.Vault, lease.AccountKeySession, cancellationToken);
    }
}
