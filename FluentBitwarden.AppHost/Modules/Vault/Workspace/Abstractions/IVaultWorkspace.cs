using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

internal interface IVaultWorkspace
{
    ValueTask OpenAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        bool forceSync,
        CancellationToken cancellationToken);

    Task<VaultSyncResult> SyncAsync(
        BitwardenAccountContext accountContext,
        bool force = false,
        CancellationToken cancellationToken = default);

    ValueTask<VaultCipher> SaveCipherAsync(
        BitwardenAccountContext accountContext,
        VaultCipher cipher,
        CancellationToken cancellationToken = default);

    void Close();
}
