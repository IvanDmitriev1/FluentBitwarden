namespace FluentBitwarden.Contracts.Session.Models;

[MemoryPackable]
public readonly partial record struct EnableWindowsHelloRequest(IntPtr OwnerWindowHandle) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.WindowsHello.Enable;
}