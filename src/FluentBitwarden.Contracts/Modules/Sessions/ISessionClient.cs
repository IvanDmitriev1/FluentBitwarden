using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Vault;

namespace FluentBitwarden.Contracts.Modules.Sessions;

/// <summary>
/// Session lifecycle operations: which account is unlocked, unlocking and locking.
/// Account CRUD lives on <see cref="Accounts.IAccountsClient"/>; vault data operations
/// live on <see cref="Vault.IVaultClient"/>.
/// </summary>
public interface ISessionClient
{
    ValueTask<AccountProfile?> GetUnlockedAccount(CancellationToken cancellationToken = default);

    ValueTask<VaultSessionStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    ValueTask<AccountUnlockOutcome> UnlockAsync(
        AccountUnlockRequest request,
        CancellationToken cancellationToken = default);

    ValueTask LockAsync(CancellationToken cancellationToken = default);
}
