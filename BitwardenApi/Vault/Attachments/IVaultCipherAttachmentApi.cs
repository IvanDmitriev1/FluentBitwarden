using BitwardenApi.Vault.Attachments.Contracts;

namespace BitwardenApi.Vault.Attachments;

public interface IVaultCipherAttachmentApi
{
    Task DownloadToAsync(
        BitwardenAccountContext accountContext,
        VaultCipherAttachment attachment,
        Func<Stream, Task> streamHandler,
        CancellationToken cancellationToken = default);
}
