namespace FluentBitwarden.Contracts.Session.Models;

public abstract record AccountUnlockOutcome
{
    private AccountUnlockOutcome() { }

    public sealed record Success() : AccountUnlockOutcome;
    public sealed record WindowsHelloCancelled() : AccountUnlockOutcome;
    public sealed record RequiresOnlineReauth() : AccountUnlockOutcome;
    public sealed record Failure(string Reason) : AccountUnlockOutcome;
}