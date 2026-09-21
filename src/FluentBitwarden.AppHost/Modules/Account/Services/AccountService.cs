using FluentBitwarden.AppHost.Modules.Account.Contracts;

namespace FluentBitwarden.AppHost.Modules.Account.Services;

internal sealed class AccountService : IAccountService
{
    public AccountProfile[] GetAccounts()
    {
        throw new NotImplementedException();
    }

    public AccountProfile? GetAccount(UserId userId)
    {
        throw new NotImplementedException();
    }

    public AccountKeyUnlockResult UnlockKey(UserId userId, AccountUnlockMethod method)
    {
        throw new NotImplementedException();
    }
}
