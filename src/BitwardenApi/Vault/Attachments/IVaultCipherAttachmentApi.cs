using BitwardenApi.Vault.Attachments.Contracts;

namespace BitwardenApi.Vault.Attachments;

public interface IVaultCipherAttachmentApi
{
    Task DownloadToAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        AttachmentId attachmentId,
        VaultCipherAttachmentStreamHandler streamHandler,
        CancellationToken cancellationToken = default);
}
