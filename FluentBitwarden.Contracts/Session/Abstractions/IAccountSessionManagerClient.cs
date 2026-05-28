using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.Contracts.Session.Abstractions;

public interface IAccountSessionManagerClient
{
    ValueTask<bool> HasActiveSession();

    ValueTask<AccountLoginOutcome> LogInAsync(AccountLoginRequest request, CancellationToken cancellationToken);
    ValueTask<GetAccountsResponse> GetAccounts();
    ValueTask<AccountUnlockOutcome> Unlock(AccountUnlockRequest request);
}
