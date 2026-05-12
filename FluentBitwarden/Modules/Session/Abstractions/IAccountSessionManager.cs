using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

public interface IAccountSessionManager
{
    AccountSession? ActiveSession { get; }
    AccountSession RequireActiveSession { get; }
    ValueTask<AccountSessionTokens> GetValidActiveSessionTokensAsync(CancellationToken cancellationToken);

    Task<AccountLoginnOutcome> SignInAsync(AccountLoginRequest request, CancellationToken cancellationToken);

    IReadOnlyList<AccountProfile> GetAccounts();
    AccountUnlockOutcome Unlock(AccountUnlockRequest request);

    void Lock();
    void Logout();
}