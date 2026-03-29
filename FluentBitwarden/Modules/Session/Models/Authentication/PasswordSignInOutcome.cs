using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Session.Models.Authentication;

public abstract record PasswordSignInOutcome
{
    private PasswordSignInOutcome() { }

    public sealed record Success(AuthenticationSuccess AuthenticationSuccess) : PasswordSignInOutcome;
    public sealed record TwoFactorRequired(
        TwoFactorChallenge Challenge,
        string Email,
        string ServerAuthorizationHash) : PasswordSignInOutcome;
    public sealed record InvalidCredentials(string Message) : PasswordSignInOutcome;
    public sealed record DeviceVerificationRequired(string Message) : PasswordSignInOutcome;
}
