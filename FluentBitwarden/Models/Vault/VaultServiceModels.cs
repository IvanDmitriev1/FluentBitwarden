using BitwaredApi.Models.Vault;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Models.Vault;

public abstract record VaultSessionState
{
    private VaultSessionState() { }

    public sealed record NoSession : VaultSessionState;
    public sealed record Locked(StoredSessionInfo Session) : VaultSessionState;
    public sealed record Unlocked(StoredSessionInfo Session) : VaultSessionState;
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
    public sealed record DecryptionFailed(string Message) : VaultReadOutcome<T>;
    public sealed record Locked(string Message) : VaultReadOutcome<T>;
    public sealed record NoCachedData(string Message) : VaultReadOutcome<T>;
    public sealed record Unavailable(string Message) : VaultReadOutcome<T>;
}

public abstract record VaultConfigurationOutcome
{
    private VaultConfigurationOutcome() { }

    public sealed record Success : VaultConfigurationOutcome;
    public sealed record InvalidInput(string Message) : VaultConfigurationOutcome;
    public sealed record Unavailable(string Message) : VaultConfigurationOutcome;
    public sealed record Cancelled(string Message) : VaultConfigurationOutcome;
}

public enum UnlockMethodStatus
{
    Unavailable = 0,
    Available = 1,
    Configured = 2,
}

public sealed record LocalUnlockStatus(
    bool HasLocalVaultData,
    UnlockMethodStatus WindowsHello,
    UnlockMethodStatus Pin)
{
    public static LocalUnlockStatus Empty { get; } = new(
        false,
        UnlockMethodStatus.Unavailable,
        UnlockMethodStatus.Unavailable);
}
