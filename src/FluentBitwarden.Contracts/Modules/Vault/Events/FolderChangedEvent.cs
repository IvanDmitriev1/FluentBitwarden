namespace FluentBitwarden.Contracts.Modules.Vault.Events;

/// <summary>Contains the committed folder state to upsert in the UI.</summary>
[MemoryPackable]
public readonly partial record struct FolderChangedEvent(
    [property: StronglyTypedIdFormatter<UserId>] UserId UserId,
    Folder Folder) : IIpcEventMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.FolderChanged;
}
