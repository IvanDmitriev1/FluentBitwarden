using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal sealed record UnlockedSession(AccountProfile Account, DecryptedUserKey UserKey) : IDisposable
{
    public AccountProfile Account { get; } = Account;
    public DecryptedUserKey UserKey { get; } = UserKey;

    public void Dispose() => UserKey.Dispose();
}
