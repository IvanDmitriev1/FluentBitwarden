using FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Models;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.ApiAccess;

internal interface IAccountAuthTokenProvider
{
    ValueTask<AccountAuthenticationTokens> GetValidTokensAsync(AccountProfile account, CancellationToken ct = default);
}