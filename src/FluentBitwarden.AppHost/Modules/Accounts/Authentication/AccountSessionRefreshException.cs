namespace FluentBitwarden.AppHost.Modules.Accounts.Authentication;

internal sealed class AccountSessionRefreshException(TokenExchangeOutcome outcome) : Exception
{
    public TokenExchangeOutcome Outcome { get; } = outcome;
}
