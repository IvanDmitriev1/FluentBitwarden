namespace FluentBitwarden.Contracts.Modules.Vault.Ciphers.Attachments;

[MemoryPackable]
public readonly partial record struct DownloadVaultCipherAttachmentRequest(
    [property: StronglyTypedIdFormatter<CipherId>] CipherId CipherId,
    [property: StronglyTypedIdFormatter<AttachmentId>] AttachmentId AttachmentId,
    string DestinationPath) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.DownloadCipherAttachment;
}
