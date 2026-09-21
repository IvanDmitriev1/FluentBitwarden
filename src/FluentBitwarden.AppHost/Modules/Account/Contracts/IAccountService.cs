namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public interface IAccountService
{
    AccountProfile[] GetAccounts();
    AccountProfile? GetAccount(UserId userId);

    AccountKeyUnlockResult UnlockKey(UserId userId, AccountUnlockMethod method);
}
