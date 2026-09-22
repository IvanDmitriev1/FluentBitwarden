using FluentBitwarden.Contracts.Modules.Accounts.Login;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public interface IAccountService
{
    AccountProfile[] GetAccounts();
    AccountProfile? GetAccount(UserId userId);

    Task LoginAsync(AccountLoginRequest request, CancellationToken cancellationToken);
    AccountKeyUnlockResult UnlockKey(UserId userId, AccountUnlockMethod method);
}
