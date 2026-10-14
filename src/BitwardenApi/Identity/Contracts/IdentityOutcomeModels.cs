namespace BitwardenApi.Identity.Contracts;

public abstract record SessionTokenResult<T>
{
    private SessionTokenResult() { }

    public sealed record Success(T Value) : SessionTokenResult<T>;
    public sealed record Rejected(SessionTokenRejection Error) : SessionTokenResult<T>;
}

public sealed record SessionTokenRejection(
    SessionTokenRejectionKind Kind,
    string Message,
    IdentityTwoFactorChallenge? TwoFactorChallenge = null);

public enum SessionTokenRejectionKind
{
    InvalidCredentials,
    TwoFactorRequired,
    DeviceVerificationRequired
}
