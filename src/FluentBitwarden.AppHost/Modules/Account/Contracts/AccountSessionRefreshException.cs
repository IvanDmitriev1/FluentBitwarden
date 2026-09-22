using BitwardenApi.Identity.Contracts;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public sealed class AccountSessionRefreshException(SessionTokenResult<TokenRefreshSessionModel> outcome) : Exception
{
    public SessionTokenResult<TokenRefreshSessionModel> Outcome { get; } = outcome;
}
