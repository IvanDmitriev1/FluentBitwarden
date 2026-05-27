namespace FluentBitwarden.Contracts.Vault.Models;

public enum VaultSyncResult
{
    NoChanges = 0,
    Synced = 1,
    SkippedOffline = 2,
    Failed = 3,
}