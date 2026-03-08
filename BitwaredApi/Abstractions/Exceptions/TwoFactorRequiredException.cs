using BitwaredApi.Models.Auth;

namespace BitwaredApi.Abstractions.Exceptions;

public sealed class TwoFactorRequiredException : Exception
{
    public TwoFactorRequiredException(TwoFactorChallenge challenge)
        : base("Two-factor authentication is required to continue.")
    {
        Challenge = challenge;
    }

    public TwoFactorChallenge Challenge { get; }
}
