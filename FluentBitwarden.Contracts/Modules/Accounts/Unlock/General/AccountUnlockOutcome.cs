namespace FluentBitwarden.Contracts.Modules.Accounts.Unlock.General;

[MemoryPackable]
[MemoryPackUnion(0, typeof(Success))]
[MemoryPackUnion(1, typeof(WindowsHelloCancelled))]
[MemoryPackUnion(2, typeof(RequiresOnlineReauth))]
[MemoryPackUnion(3, typeof(Failure))]
public abstract partial record AccountUnlockOutcome
{
    private AccountUnlockOutcome() { }

    [MemoryPackable]
    public sealed partial record Success() : AccountUnlockOutcome;

    [MemoryPackable]
    public sealed partial record WindowsHelloCancelled() : AccountUnlockOutcome;

    [MemoryPackable]
    public sealed partial record RequiresOnlineReauth() : AccountUnlockOutcome;

    [MemoryPackable]
    public sealed partial record Failure(string Reason) : AccountUnlockOutcome;
}