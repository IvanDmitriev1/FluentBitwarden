using FluentBitwarden.Contracts.Modules.Vault.Operations;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultSyncService
{
    Task<VaultSyncResult> SyncAsync();
}
