using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal sealed record UnlockedSession(AccountProfile Account, UserKey UserKey) : IDisposable
{
    public AccountProfile Account { get; } = Account;
    public UserKey UserKey { get; } = UserKey;

    public void Dispose() => UserKey.Dispose();
}
