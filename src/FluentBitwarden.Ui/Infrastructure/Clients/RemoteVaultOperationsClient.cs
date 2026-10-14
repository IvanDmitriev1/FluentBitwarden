using FluentBitwarden.Contracts.Modules.Vault.Operations;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.Infrastructure.Clients;

internal sealed class RemoteVaultOperationsClient(IIpcClient client) : IVaultOperationsClient
{
    public Task<VaultSyncResult> SyncAsync(
        SyncVaultRequest request,
        CancellationToken cancellationToken = default) =>
        client.SendAsync<SyncVaultRequest, VaultSyncResult>(request, cancellationToken);
}
