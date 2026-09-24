namespace FluentBitwarden.Contracts.AppSession.State;

[MemoryPackable]
public readonly partial record struct GetAppSessionStateRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Session.GetState;
}
