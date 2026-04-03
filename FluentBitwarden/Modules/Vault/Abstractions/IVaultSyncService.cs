namespace FluentBitwarden.Modules.Vault.Abstractions;

public interface IVaultSyncService
{
    Task<bool> SyncVaultAsync();
}