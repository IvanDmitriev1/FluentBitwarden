using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Sessions.Models;

internal sealed class SessionSnapshot : IDisposable
{
    public SessionSnapshot(AccountProfile account, UserKey userKey, KeySession keys, IUnlockedVault vault)
    {
        ThrowIfVaultBelongsToAnotherAccount(account, vault);

        Account = account;
        UserKey = userKey;
        Keys = keys;
        _vault = vault;
    }

    private IUnlockedVault _vault;

    public AccountProfile Account { get; }

    public UserKey UserKey { get; }

    public KeySession Keys { get; }

    /// <summary>The session's decrypted vault. Never null.</summary>
    public IUnlockedVault Vault => Volatile.Read(ref _vault);

    /// <summary>
    /// Swaps in a vault produced by a sync or a save. Call only inside
    /// <see cref="IVaultSessionManager.WithSessionAsync"/> — the transition gate is what keeps the
    /// swap ordered against unlock and lock.
    /// </summary>
    public void ReplaceVault(IUnlockedVault vault)
    {
        ThrowIfVaultBelongsToAnotherAccount(Account, vault);
        Volatile.Write(ref _vault, vault);
    }

    public void Dispose()
    {
        Keys.Dispose();
        UserKey.Dispose();
    }

    private static void ThrowIfVaultBelongsToAnotherAccount(AccountProfile account, IUnlockedVault vault)
    {
        if (vault.UserId != account.UserId)
        {
            throw new InvalidOperationException(
                $"Vault belongs to user '{vault.UserId}' but the session is for user '{account.UserId}'.");
        }
    }
}
