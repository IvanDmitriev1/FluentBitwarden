namespace FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Models.Exceptions;

internal sealed class AccountSessionRefreshException(TokenExchangeOutcome outcome) : Exception
{
    public TokenExchangeOutcome Outcome { get; } = outcome;
}