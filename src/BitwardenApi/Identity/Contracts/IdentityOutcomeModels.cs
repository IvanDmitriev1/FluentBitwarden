namespace BitwardenApi.Identity.Contracts;

public abstract record TokenExchangeOutcome
{
    private TokenExchangeOutcome() { }

    public sealed record Authenticated(TokenAuthenticatedModel AuthenticatedModel) : TokenExchangeOutcome;
    public sealed record SessionRefreshed(TokenRefreshSessionModel Session) : TokenExchangeOutcome;
    public sealed record TwoFactorRequired(IdentityTwoFactorChallenge Challenge, string Message) : TokenExchangeOutcome;
    public sealed record InvalidCredentials(string Message) : TokenExchangeOutcome;
    public sealed record DeviceVerificationRequired(string Message) : TokenExchangeOutcome;
}
