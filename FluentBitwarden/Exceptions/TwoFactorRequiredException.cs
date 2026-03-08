using BitwaredApi.Models.Auth;

namespace FluentBitwarden.Exceptions;

public sealed class TwoFactorRequiredException(TwoFactorChallenge challenge)
    : Exception("Two-factor authentication is required to continue.")
{
    public TwoFactorChallenge Challenge { get; } = challenge;
}
