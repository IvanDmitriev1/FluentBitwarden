using FluentBitwarden.Contracts.AppSession.Status;
using FluentBitwarden.Contracts.AppSession.Unlock;
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
    ValueTask<AppSessionSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    ValueTask<SessionUnlockOutcome> UnlockAsync(
        SessionUnlockRequest request,
        CancellationToken cancellationToken = default);

    ValueTask LockAsync(CancellationToken cancellationToken = default);
}
