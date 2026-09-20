namespace FluentBitwarden.Contracts.AppSession.Status;

[MemoryPackable]
public readonly partial record struct AppSesstionStatusChangedEvent(AppSessionSnapshot Snapshot) : IIpcEventMessage
{
    public static ushort MessageType => IpcMessageTypes.Session.StateChanged;
}
