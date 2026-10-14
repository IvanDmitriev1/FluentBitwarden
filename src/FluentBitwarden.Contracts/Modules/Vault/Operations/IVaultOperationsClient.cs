namespace FluentBitwarden.Contracts.Modules.Vault.Operations;

public interface IVaultOperationsClient
{
    Task<VaultSyncResult> SyncAsync(
        SyncVaultRequest request,
        CancellationToken cancellationToken = default);
}
