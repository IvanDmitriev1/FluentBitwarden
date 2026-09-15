using BitwardenApi.Vault.Attachments.Contracts;

namespace BitwardenApi.Vault.Attachments;

public interface IVaultCipherAttachmentApi
{
    Task DownloadToAsync(
        BitwardenAccountContext accountContext,
        VaultCipherAttachment attachment,
        VaultCipherAttachmentStreamHandler streamHandler,
        CancellationToken cancellationToken = default);
}
