using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Session.Models;

public abstract record AccountLoginnOutcome
{
    private AccountLoginnOutcome() { }

    public sealed record Success(AccountSignInSuccess AccountSignInSuccess) : AccountLoginnOutcome;
    public sealed record TwoFactorRequired(
        TwoFactorChallenge Challenge,
        string Email,
        string ServerAuthorizationHash) : AccountLoginnOutcome;
    public sealed record InvalidCredentials(string Message) : AccountLoginnOutcome;
    public sealed record DeviceVerificationRequired(string Message) : AccountLoginnOutcome;
}