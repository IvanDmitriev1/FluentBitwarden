namespace FluentBitwarden.Contracts.Modules.Vault.Events;

/// <summary>Contains the committed cipher state to upsert in the UI.</summary>
[MemoryPackable]
public readonly partial record struct CipherChangedEvent(
    [property: StronglyTypedIdFormatter<UserId>] UserId UserId,
    Cipher Cipher) : IIpcEventMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.CipherChanged;
}
