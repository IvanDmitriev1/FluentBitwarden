using BitwaredApi.Models.Auth;

namespace BitwaredApi.Abstractions.Exceptions;

public sealed class TokenEndpointException : Exception
{
    public TokenEndpointException(
        string? error,
        string? errorDescription,
        bool deviceVerificationRequired = false,
        TwoFactorChallenge? twoFactorChallenge = null)
        : base(errorDescription ?? error ?? "The token endpoint rejected the request.")
    {
        Error = error;
        ErrorDescription = errorDescription;
        DeviceVerificationRequired = deviceVerificationRequired;
        TwoFactorChallenge = twoFactorChallenge;
    }

    public string? Error { get; }

    public string? ErrorDescription { get; }

    public bool DeviceVerificationRequired { get; }

    public TwoFactorChallenge? TwoFactorChallenge { get; }
}
