namespace BitwaredApi.Models.Auth;

public abstract record TokenExchangeOutcome
{
    private TokenExchangeOutcome()
    {
    }

    public sealed record Success(TokenResponseModel Response) : TokenExchangeOutcome;

    public sealed record TwoFactorRequired(TwoFactorChallenge Challenge, string Message) : TokenExchangeOutcome;

    public sealed record InvalidCredentials(string Message) : TokenExchangeOutcome;

    public sealed record DeviceVerificationRequired(string Message) : TokenExchangeOutcome;
}

public sealed record AuthRequestApproval(
    string EncryptedUserKey,
    DateTimeOffset? ResponseDate,
    string? RequestDeviceIdentifier,
    string? RequestIpAddress,
    string? RequestCountryName);

public abstract record AuthRequestPollOutcome
{
    private AuthRequestPollOutcome()
    {
    }

    public sealed record Pending : AuthRequestPollOutcome;

    public sealed record Approved(AuthRequestApproval Approval) : AuthRequestPollOutcome;

    public sealed record Denied(string Message) : AuthRequestPollOutcome;

    public sealed record Expired(string Message) : AuthRequestPollOutcome;
}
