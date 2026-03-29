using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Session.Models.Exceptions;

internal sealed class SessionRefreshException(TokenExchangeOutcome outcome) : Exception
{
    public TokenExchangeOutcome Outcome { get; } = outcome;
}