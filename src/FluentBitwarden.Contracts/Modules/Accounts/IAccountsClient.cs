using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Contracts.Modules.Accounts;

/// <summary>
/// Stored-account operations. Session lifecycle (unlock/lock/status) lives on
/// <see cref="IAppSessionClient"/>.
/// </summary>
public interface IAccountsClient
{
    Task<AccountProfile[]> GetAccountsAsync(
        GetAccountsRequest request,
        CancellationToken cancellationToken = default);

    Task<AccountAuthenticationOutcome> AuthenticateAsync(
        AccountAuthenticationRequest request,
        CancellationToken cancellationToken = default);
}
