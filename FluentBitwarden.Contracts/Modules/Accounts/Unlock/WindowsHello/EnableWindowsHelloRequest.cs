namespace FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;

[MemoryPackable]
public readonly partial record struct EnableWindowsHelloRequest(IntPtr OwnerWindowHandle) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.WindowsHello.Enable;
}