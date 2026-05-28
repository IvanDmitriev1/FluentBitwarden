using BitwardenApi.Models;

namespace FluentBitwarden.Contracts.Session.Models;

[MemoryPackable]
public readonly partial record struct GetWindowsHelloStatusRequest(UserId UserId) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.WindowsHello.GetAccountStatus;
}
