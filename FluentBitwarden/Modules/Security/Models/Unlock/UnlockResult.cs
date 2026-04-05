using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Security.Models.Unlock;

public abstract record UnlockResult
{
    private UnlockResult() {}

    public sealed record Success(DecryptedUserKey userKey) : UnlockResult;
    public sealed record PinLocked() : UnlockResult;
    public sealed record WindowsHelloCancelled() : UnlockResult;
    public sealed record RequiresOnlineReauth() : UnlockResult;
    public sealed record Failure(string Reason) : UnlockResult;
}