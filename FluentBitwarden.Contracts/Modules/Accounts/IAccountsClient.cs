using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;

namespace FluentBitwarden.Contracts.Modules.Accounts;

public interface IAccountsClient
{
    ValueTask<AccountProfile?> GetUnlockedAccount(CancellationToken cancellationToken = default);

    ValueTask<AccountProfileDetails?> GetUnlockedAccountProfileDetails(CancellationToken cancellationToken = default);

    ValueTask<AccountProfile[]> GetAccountsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<AccountLoginOutcome> LoginAsync(
        AccountLoginRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<AccountUnlockOutcome> UnlockAsync(
        AccountUnlockRequest request,
        CancellationToken cancellationToken = default);
}
