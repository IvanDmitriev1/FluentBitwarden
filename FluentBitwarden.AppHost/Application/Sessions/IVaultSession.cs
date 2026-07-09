using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Vault;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Application.Sessions;

internal interface IVaultSession
{
    event Action<VaultSessionStatus> SessionStatusChanged;

    bool TryGetUnlockedSession([NotNullWhen(true)] out SessionSnapshot? session);
    SessionSnapshot GetUnlockedSession();

    ValueTask<AccountUnlockOutcome> UnlockAsync(AccountUnlockRequest request, CancellationToken cancellationToken);

    /// <summary>Locks the vault. A lock requested while an unlock is in flight wins: the unlock is aborted.</summary>
    Task LockAsync(CancellationToken cancellationToken = default);

    /// <summary>Fire-and-forget wrapper over <see cref="LockAsync"/> for non-async callers; failures are logged.</summary>
    void RequestLock();
}
