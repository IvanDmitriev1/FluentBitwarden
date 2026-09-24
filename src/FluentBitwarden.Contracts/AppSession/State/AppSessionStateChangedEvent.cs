namespace FluentBitwarden.Contracts.AppSession.State;

[MemoryPackable]
public readonly partial record struct AppSessionStateChangedEvent(AppSessionState State) : IIpcEventMessage
{
    public static ushort MessageType => IpcMessageTypes.Session.StateChanged;
}
