using FluentBitwarden.Contracts.Infrastructure.WindowsHello;

namespace FluentBitwarden.Contracts.AppSession.Unlock;

[MemoryPackable]
[MemoryPackUnion(0, typeof(MasterPasswordRequest))]
[MemoryPackUnion(1, typeof(WindowsHelloRequest))]
public abstract partial record SessionUnlockRequest(UserId UserId)
{
    public static ushort MessageType => IpcMessageTypes.Session.Unlock;

    [MemoryPackable]
    public sealed partial record MasterPasswordRequest(UserId UserId, string MasterPassword) : SessionUnlockRequest(UserId);

    [MemoryPackable]
    public sealed partial record WindowsHelloRequest(UserId UserId, NativeWindowHandle OwnerWindow) : SessionUnlockRequest(UserId);
}
