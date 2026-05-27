using BitwardenApi.Models;

namespace FluentBitwarden.Contracts.Session.Models;

public abstract record AccountLoginOutcome
{
    private AccountLoginOutcome() { }

    public sealed record Success() : AccountLoginOutcome;
    public sealed record TwoFactorRequired(
        TwoFactorChallenge Challenge,
        string Email,
        string ServerAuthorizationHash) : AccountLoginOutcome;
    public sealed record InvalidCredentials(string Message) : AccountLoginOutcome;
    public sealed record DeviceVerificationRequired(string Message) : AccountLoginOutcome;
}