using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class RemoteVaultOperationsClient(IIpcClient ipcClient) : IVaultOperationsClient
{
    public Task<VaultSyncResult> SyncAsync(
        SyncVaultRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<SyncVaultRequest, VaultSyncResult>(request, cancellationToken);
}
