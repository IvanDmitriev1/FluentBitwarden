namespace FluentBitwarden.Contracts.AppSession.Unlock;

[MemoryPackable]
[MemoryPackUnion(0, typeof(Success))]
[MemoryPackUnion(1, typeof(WindowsHelloCancelled))]
[MemoryPackUnion(2, typeof(RequiresOnlineReauth))]
[MemoryPackUnion(3, typeof(Failure))]
public abstract partial record SessionUnlockOutcome
{
    private SessionUnlockOutcome() { }

    [MemoryPackable]
    public sealed partial record Success : SessionUnlockOutcome;

    [MemoryPackable]
    public sealed partial record WindowsHelloCancelled : SessionUnlockOutcome;

    [MemoryPackable]
    public sealed partial record RequiresOnlineReauth : SessionUnlockOutcome;

    [MemoryPackable]
    public sealed partial record Failure(string Reason) : SessionUnlockOutcome;
}
