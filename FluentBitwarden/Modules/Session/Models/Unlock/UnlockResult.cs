namespace FluentBitwarden.Modules.Session.Models.Unlock;

public abstract record UnlockResult
{
    private UnlockResult() {}

    public sealed record Success(UserKeySession Session) : UnlockResult;
    public sealed record InvalidCredential(int Remaining) : UnlockResult;
    public sealed record PinLocked() : UnlockResult;
    public sealed record WindowsHelloCancelled() : UnlockResult;
    public sealed record RequiresOnlineReauth() : UnlockResult;
    public sealed record Failure(string Reason) : UnlockResult;
}