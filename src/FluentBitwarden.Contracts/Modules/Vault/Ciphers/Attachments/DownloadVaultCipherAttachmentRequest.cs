using BitwardenApi.Vault.Attachments.Contracts;

namespace FluentBitwarden.Contracts.Modules.Vault.Ciphers.Attachments;

[MemoryPackable]
public readonly partial record struct DownloadVaultCipherAttachmentRequest(
    VaultCipherAttachment Attachment,
    string DestinationPath) : IIpcRequestMessage
{
    public static ushort MessageType => IpcMessageTypes.Vault.DownloadCipherAttachment;
}