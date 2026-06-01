namespace FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;

[MemoryPackable]
public readonly partial record struct GetWindowsHelloStatusRequest(UserId UserId) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.WindowsHello.GetAccountStatus;
}
