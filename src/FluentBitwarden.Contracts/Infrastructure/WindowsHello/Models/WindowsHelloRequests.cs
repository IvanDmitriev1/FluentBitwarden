namespace FluentBitwarden.Contracts.Infrastructure.WindowsHello.Models;

[MemoryPackable]
public readonly partial record struct GetWindowsHelloEnrollmentRequest(
    UserId UserId) : IIpcRequestMessage
{
    public static ushort MessageType =>
        IpcMessageTypes.WindowsHello.GetEnrollment;
}

[MemoryPackable]
public readonly partial record struct EnableWindowsHelloEnrollmentRequest(
    UserId UserId,
    NativeWindowHandle OwnerWindow) : IIpcRequestMessage
{
    public static ushort MessageType =>
        IpcMessageTypes.WindowsHello.EnableEnrollment;
}

[MemoryPackable]
public readonly partial record struct DisableWindowsHelloEnrollmentRequest(
    UserId UserId) : IIpcRequestMessage
{
    public static ushort MessageType =>
        IpcMessageTypes.WindowsHello.DisableEnrollment;
}
