using BitwardenApi.Identity.Models;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public sealed class AccountSessionRefreshException(SessionTokenResult<RefreshedSession> outcome) : Exception
{
    public SessionTokenResult<RefreshedSession> Outcome { get; } = outcome;
}
