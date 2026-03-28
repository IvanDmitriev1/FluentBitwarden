namespace BitwardenApi.Modules.Identity.Models;

public abstract record TokenExchangeOutcome
{
    private TokenExchangeOutcome() { }

    public sealed record Success(TokenResponseModel Response) : TokenExchangeOutcome;
    public sealed record TwoFactorRequired(TwoFactorChallenge Challenge, string Message) : TokenExchangeOutcome;
    public sealed record InvalidCredentials(string Message) : TokenExchangeOutcome;
    public sealed record DeviceVerificationRequired(string Message) : TokenExchangeOutcome;
}
