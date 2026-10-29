namespace FluentBitwarden.Contracts.AppSession.State;

[MemoryPackable]
public readonly partial record struct AppSessionStateChangedEvent(AppSessionStateSnapshot StateSnapshot) : IIpcEventMessage
{
    public static ushort MessageType => IpcMessageTypes.Session.StateChanged;
}
