using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Unlock;

internal sealed record UnlockedSession(AccountProfile Account, DecryptedUserKey UserKey) : IDisposable
{
    public BitwardenAccountContext AccountContext => new(Account.UserId, Account.Environment);

    public void Dispose() => UserKey.Dispose();
}
