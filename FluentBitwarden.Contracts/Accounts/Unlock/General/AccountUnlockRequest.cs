namespace FluentBitwarden.Contracts.Session.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(MasterPasswordRequest))]
[MemoryPackUnion(1, typeof(WindowsHelloRequest))]
public abstract partial record AccountUnlockRequest(AccountProfile Account) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Account.Unlock;

    [MemoryPackable]
    public sealed partial record MasterPasswordRequest(AccountProfile Account, string MasterPassword) : AccountUnlockRequest(Account);

    [MemoryPackable]
    public sealed partial record WindowsHelloRequest(AccountProfile Account, IntPtr OwnerWindowHandle) : AccountUnlockRequest(Account);
}