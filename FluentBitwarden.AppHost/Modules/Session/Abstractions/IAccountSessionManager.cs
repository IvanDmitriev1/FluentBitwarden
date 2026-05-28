using FluentBitwarden.Contracts.Session.Models;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

internal interface IAccountSessionManager
{
    AccountSession? ActiveSession { get; }
    AccountSession RequireActiveSession { get; }
    ValueTask<AccountSessionTokens> GetValidActiveSessionTokensAsync(CancellationToken cancellationToken);

    Task<AccountLoginOutcome> LogInAsync(AccountLoginRequest request, CancellationToken cancellationToken);

    AccountProfile[] GetAccounts();
    AccountUnlockOutcome Unlock(AccountUnlockRequest request);

    void Lock();
    void Logout();
}