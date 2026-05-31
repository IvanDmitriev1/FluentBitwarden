namespace FluentBitwarden.Contracts.Modules.Vault.Models;

public enum VaultSyncResult
{
    NoChanges = 0,
    Synced = 1,
    SkippedOffline = 2,
    Failed = 3,
}