using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Contracts.Modules.Accounts.Login;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public interface IAccountService
{
    AccountProfile[] GetAccounts();
    AccountProfile? GetAccount(UserId userId);

    Task<AccountAuthenticationOutcome> AuthenticateAsync(AccountAuthenticationRequest request, CancellationToken cancellationToken);
    AccountKeyUnlockResult UnlockKey(UserId userId, AccountUnlockMethod method);
}
