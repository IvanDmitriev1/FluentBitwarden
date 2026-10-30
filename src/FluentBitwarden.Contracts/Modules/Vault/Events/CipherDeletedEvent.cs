namespace FluentBitwarden.Contracts.Modules.Vault.Events;

/// <summary>Identifies a cipher permanently removed from the local vault.</summary>
[MemoryPackable]
public readonly partial record struct CipherDeletedEvent(
    [property: StronglyTypedIdFormatter<UserId>] UserId UserId,
    [property: StronglyTypedIdFormatter<CipherId>] CipherId CipherId) : IIpcEventMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.CipherDeleted;
}
