using FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Models;
using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.AppHost.Modules.Accounts.ApiAccess;

internal interface IAccountAuthTokenProvider
{
    ValueTask<AccountAuthenticationTokens> GetValidTokensAsync(AccountProfile account, CancellationToken ct = default);
}