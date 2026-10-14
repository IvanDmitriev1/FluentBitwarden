using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Contracts.AppSession.State;

[MemoryPackable]
[MemoryPackUnion(0, typeof(NotAuthenticated))]
[MemoryPackUnion(1, typeof(Locked))]
[MemoryPackUnion(2, typeof(Unlocked))]
public abstract partial record AppSessionState
{
    private AppSessionState() { }

    [MemoryPackable]
    public sealed partial record NotAuthenticated : AppSessionState;

    [MemoryPackable]
    public sealed partial record Locked(AccountProfile Account) : AppSessionState;

    [MemoryPackable]
    public sealed partial record Unlocked(AccountProfile Account) : AppSessionState;
}
