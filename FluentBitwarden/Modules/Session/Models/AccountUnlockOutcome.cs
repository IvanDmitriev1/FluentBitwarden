using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Session.Models;

public abstract record AccountUnlockOutcome
{
    private AccountUnlockOutcome() { }

    public sealed record Success(DecryptedUserKey UserKey) : AccountUnlockOutcome;
    public sealed record WindowsHelloCancelled() : AccountUnlockOutcome;
    public sealed record RequiresOnlineReauth() : AccountUnlockOutcome;
    public sealed record Failure(string Reason) : AccountUnlockOutcome;
}