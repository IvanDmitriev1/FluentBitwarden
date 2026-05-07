using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Session.Models;

public abstract record AccountSignInOutcome
{
    private AccountSignInOutcome() { }

    public sealed record Success(AccountSignInSuccess AccountSignInSuccess) : AccountSignInOutcome;
    public sealed record TwoFactorRequired(
        TwoFactorChallenge Challenge,
        string Email,
        string ServerAuthorizationHash) : AccountSignInOutcome;
    public sealed record InvalidCredentials(string Message) : AccountSignInOutcome;
    public sealed record DeviceVerificationRequired(string Message) : AccountSignInOutcome;
}