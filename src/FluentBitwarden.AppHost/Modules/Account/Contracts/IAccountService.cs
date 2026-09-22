using BitwardenApi.Primitives;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public interface IAccountService
{
    AccountProfile[] GetAccounts();
    AccountProfile? GetAccount(UserId userId);

    Task<AccountAuthenticationOutcome> AuthenticateAsync(AccountAuthenticationRequest request, CancellationToken cancellationToken);
    Task<AccountSessionTokens> RefreshSessionTokens(BitwardenAccountContext accountContext, CancellationToken cancellationToken);
    AccountKeyUnlockResult UnlockKey(UserId userId, AccountUnlockMethod method);
}
