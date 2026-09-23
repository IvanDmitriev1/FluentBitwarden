using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.AppSession.Lock;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Vault;

namespace FluentBitwarden.Contracts.AppSession;

/// <summary>
/// Session lifecycle operations: which account is unlocked, unlocking and locking.
/// Account CRUD lives on <see cref="IAccountsClient"/>; vault data operations
/// live on <see cref="IVaultClient"/>.
/// </summary>
public interface IAppSessionClient
{
    Task<AppSessionSnapshot> GetSnapshotAsync(
        GetAppSessionSnapshotRequest request,
        CancellationToken cancellationToken = default);

    Task<SessionUnlockOutcome> UnlockAsync(
        SessionUnlockRequest request,
        CancellationToken cancellationToken = default);

    Task LockAsync(
        LockAppSessionRequest request,
        CancellationToken cancellationToken = default);
}
