using BitwaredApi;
using BitwaredApi.Models.Vault;

namespace FluentBitwarden.Models.Vault;

public sealed record VaultState(
    bool HasStoredSession,
    string? AccountId,
    string? Email,
    BitwardenEnvironment? Environment,
    bool IsLocked,
    bool HasCachedData,
    DateTimeOffset? LastSyncUtc,
    DateTimeOffset? RevisionDate,
    bool HasLocalUnlockData,
    bool CanUnlockWithMasterPassword,
    bool IsPinConfigured,
    bool IsWindowsHelloConfigured,
    bool CanUseWindowsHello)
{
    public static VaultState Empty { get; } = new(
        false,
        null,
        null,
        null,
        true,
        false,
        null,
        null,
        false,
        false,
        false,
        false,
        false);
}

public abstract record VaultUnlockOutcome
{
    private VaultUnlockOutcome() { }

    public sealed record Success : VaultUnlockOutcome;
    public sealed record InvalidCredentials(string Message) : VaultUnlockOutcome;
    public sealed record Unavailable(string Message) : VaultUnlockOutcome;
    public sealed record Cancelled(string Message) : VaultUnlockOutcome;
}

public abstract record VaultSyncOutcome
{
    private VaultSyncOutcome() { }

    public sealed record Success(SyncSummary Summary) : VaultSyncOutcome;
    public sealed record Offline(string Message) : VaultSyncOutcome;
    public sealed record Locked(string Message) : VaultSyncOutcome;
    public sealed record Unavailable(string Message) : VaultSyncOutcome;
}

public abstract record VaultReadOutcome<T>
{
    private VaultReadOutcome() { }

    public sealed record Success(T Value) : VaultReadOutcome<T>;
    public sealed record Locked(string Message) : VaultReadOutcome<T>;
    public sealed record NoCachedData(string Message) : VaultReadOutcome<T>;
    public sealed record Unavailable(string Message) : VaultReadOutcome<T>;
}

internal abstract record VaultConfigurationOutcome
{
    private VaultConfigurationOutcome() { }

    public sealed record Success : VaultConfigurationOutcome;
    public sealed record InvalidInput(string Message) : VaultConfigurationOutcome;
    public sealed record Unavailable(string Message) : VaultConfigurationOutcome;
    public sealed record Cancelled(string Message) : VaultConfigurationOutcome;
}
