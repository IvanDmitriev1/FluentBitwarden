using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Account.Contracts;

public abstract record AccountKeyUnlockResult
{
    public sealed record Success(UnlockedUserKey UserKey) : AccountKeyUnlockResult;
    public sealed record Failure(string Error) : AccountKeyUnlockResult;
    public sealed record InvalidCredentials : AccountKeyUnlockResult;
    public sealed record Cancelled : AccountKeyUnlockResult;
    public sealed record RequiresOnlineReauthentication : AccountKeyUnlockResult;
}
