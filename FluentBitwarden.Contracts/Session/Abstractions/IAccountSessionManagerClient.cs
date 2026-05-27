using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.Contracts.Session.Abstractions;

public interface IAccountSessionManagerClient
{
    ValueTask<bool> HasActiveSession();

    ValueTask<AccountLoginOutcome> SignInAsync(AccountLoginRequest request, CancellationToken cancellationToken);
    ValueTask<IReadOnlyList<AccountProfile>> GetAccounts();
    ValueTask<AccountUnlockOutcome> Unlock(AccountUnlockRequest request);
}
