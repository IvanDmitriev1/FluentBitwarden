namespace FluentBitwarden.Modules.Vault.Models;

public abstract record UnlockResult
{
    private UnlockResult() {}

    public sealed record UnlockInvalidCredentials(UnlockMethod Method, int? RemainingAttempts = null) : UnlockResult;
    public sealed record UnlockRequiresSetup(UnlockMethod Method) : UnlockResult;
    public sealed record UnlockUnavailable(UnlockMethod Method, string Reason) : UnlockResult;
    public sealed record UnlockLockedOut(UnlockMethod Method) : UnlockResult;
}