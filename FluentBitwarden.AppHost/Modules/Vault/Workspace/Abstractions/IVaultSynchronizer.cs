using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;

internal interface IVaultSynchronizer
{
    ValueTask<VaultSyncResult> SyncAsync(DecryptedUserKey decryptedUserKey, bool force = false, CancellationToken cancellationToken = default);
}