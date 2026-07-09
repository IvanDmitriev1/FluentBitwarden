using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Application.Sessions;

/// <summary>
/// The single immutable snapshot of one unlocked session: account, user key, derived key
/// state and decrypted vault data. Owned exclusively by <see cref="VaultSession"/>;
/// every mutation replaces the whole snapshot so lock-free readers always observe a coherent view.
/// </summary>
internal sealed record SessionSnapshot(
    AccountProfile Account,
    UserKey UserKey,
    KeySession Keys,
    LoadedVaultData Data) : IDisposable
{
    public void Dispose()
    {
        Keys.Dispose();
        UserKey.Dispose();
    }
}
