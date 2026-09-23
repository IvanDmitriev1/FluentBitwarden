namespace FluentBitwarden.Contracts.AppSession.Lock;

[MemoryPackable]
public readonly partial record struct LockAppSessionRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Session.Lock;
}
