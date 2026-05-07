using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

public interface IAccountSessionManager
{
    AccountSession? ActiveSession { get; }
    AccountSession RequireActiveSession { get; }
    ValueTask<AccountSessionTokens> GetValidActiveSessionTokensAsync(CancellationToken cancellationToken);


    Task<AccountSignInOutcome> SignInAsync(AccountSignInRequest request, CancellationToken cancellationToken);
    Task UnlockAsync(CancellationToken cancellationToken);

    void Lock();
    void Logout();
}