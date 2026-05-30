using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.Contracts.Accounts;

public interface IAccountsClient
{
    ValueTask<AccountProfile?> GetUnlockedAccount();

    ValueTask<AccountProfile[]> GetAccountsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<AccountLoginOutcome> LoginAsync(
        AccountLoginRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<AccountUnlockOutcome> UnlockAsync(
        AccountUnlockRequest request,
        CancellationToken cancellationToken = default);
}