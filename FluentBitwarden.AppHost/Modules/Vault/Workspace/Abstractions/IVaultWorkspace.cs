using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

internal interface IVaultWorkspace
{
    ValueTask OpenAsync(DecryptedUserKey userKey, CancellationToken cancellationToken);
    Task<VaultSyncResult> SyncAsync(DecryptedUserKey decryptedUserKey, bool force = false, CancellationToken cancellationToken = default);
    void Reload(DecryptedUserKey userKey);
    void Close();
}
