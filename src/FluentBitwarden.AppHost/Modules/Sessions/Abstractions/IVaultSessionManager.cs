using FluentBitwarden.AppHost.Modules.Sessions.Models;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Vault;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Modules.Sessions.Abstractions;

internal interface IVaultSessionManager
{
    event Action<VaultSessionStatus> SessionStatusChanged;

    bool TryGetUnlockedSession([NotNullWhen(true)] out SessionSnapshot? session);
    SessionSnapshot GetUnlockedSession();

    Task<AccountUnlockOutcome> UnlockAsync(AccountUnlockRequest request, CancellationToken cancellationToken);

    Task LockAsync(CancellationToken cancellationToken = default);

    Task<T> WithSessionAsync<T>(
        Func<SessionSnapshot, CancellationToken, Task<T>> work,
        T lockedResult,
        CancellationToken cancellationToken);
}
