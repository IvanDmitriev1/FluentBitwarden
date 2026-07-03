using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

internal interface IVaultWorkspace
{
    ValueTask OpenAsync(
        BitwardenAccountContext accountContext,
        DecryptedUserKey userKey,
        bool forceSync,
        CancellationToken cancellationToken);

    Task<VaultSyncResult> SyncAsync(
        BitwardenAccountContext accountContext,
        DecryptedUserKey decryptedUserKey,
        bool force = false,
        CancellationToken cancellationToken = default);

    void Reload(DecryptedUserKey userKey);
    void Close();
}
