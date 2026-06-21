using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Authentication;

internal interface IAccountTokenProvider
{
    ValueTask<AccountTokens> GetValidTokensAsync(
        AccountProfile account,
        CancellationToken cancellationToken = default);
}
