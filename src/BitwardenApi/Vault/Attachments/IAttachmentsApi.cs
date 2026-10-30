
using BitwardenApi.Client;
namespace BitwardenApi.Vault.Attachments;

public interface IAttachmentsApi
{
    Task DownloadToAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        AttachmentId attachmentId,
        AttachmentStreamHandler streamHandler,
        CancellationToken cancellationToken = default);
}
