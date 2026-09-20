using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts.Login;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Contracts.Modules.Accounts;

/// <summary>
/// Stored-account operations. Session lifecycle (unlock/lock/status) lives on
/// <see cref="IAppSessionClient"/>.
/// </summary>
public interface IAccountsClient
{
    ValueTask<AccountProfile[]> GetAccountsAsync(
        CancellationToken cancellationToken = default);

    ValueTask<AccountLoginOutcome> LoginAsync(
        AccountLoginRequest request,
        CancellationToken cancellationToken = default);
}
