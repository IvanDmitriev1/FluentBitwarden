namespace FluentBitwarden.Contracts.AppSession.Status;

[MemoryPackable]
public readonly partial record struct GetAppSessionSnapshotRequest : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Session.GetSnapshot;
}
