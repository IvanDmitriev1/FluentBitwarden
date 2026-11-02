namespace FluentBitwarden.Contracts.Modules.Vault.Events;

/// <summary>Identifies a folder removed from the local vault.</summary>
[MemoryPackable]
public readonly partial record struct FolderDeletedEvent(
    [property: StronglyTypedIdFormatter<UserId>] UserId UserId,
    [property: StronglyTypedIdFormatter<FolderId>] FolderId FolderId) : IIpcEventMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.FolderDeleted;
}
