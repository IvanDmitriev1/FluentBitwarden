namespace FluentBitwarden.Contracts.Infrastructure.WindowsHello;

[MemoryPackable]
public readonly partial record struct GetWindowsHelloEnrollmentRequest(UserId UserId) : IIpcRequestMessage
{
    public static ushort MessageType =>
        IpcMessageTypes.WindowsHello.GetEnrollment;
}

[MemoryPackable]
public readonly partial record struct EnableWindowsHelloEnrollmentRequest(
    NativeWindowHandle OwnerWindow) : IIpcRequestMessage
{
    public static ushort MessageType =>
        IpcMessageTypes.WindowsHello.EnableEnrollment;
}

[MemoryPackable]
public readonly partial record struct DisableWindowsHelloEnrollmentRequest : IIpcRequestMessage
{
    public static ushort MessageType =>
        IpcMessageTypes.WindowsHello.DisableEnrollment;
}
