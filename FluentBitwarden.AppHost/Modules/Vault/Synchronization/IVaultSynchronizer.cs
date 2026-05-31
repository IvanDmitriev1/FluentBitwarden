using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Synchronization;

internal interface IVaultSynchronizer
{
    ValueTask<VaultSyncResult> SyncAsync(CancellationToken cancellationToken);
}