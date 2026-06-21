using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;

namespace FluentBitwarden.AppHost.Application.Sessions;

internal interface IVaultSessionCoordinator
{
    bool TryGetUnlockedSession([NotNullWhen(true)] out UnlockedSession? session);
    UnlockedSession GetUnlockedSession();

    ValueTask<AccountUnlockOutcome> UnlockAsync(
        AccountUnlockRequest request,
        CancellationToken cancellationToken);

    void RequestLock();

    ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken);
}
