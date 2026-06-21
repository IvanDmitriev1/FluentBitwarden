using BitwardenApi.Primitives;

namespace BitwardenApi.Vault.Attachments;

public interface IVaultAttachmentsApi
{
    Task<AttachmentUploadInit> StartUploadV2Async(
        BitwardenAccountContext accountContext,
        StartUploadV2Request request,
        CancellationToken cancellationToken = default);

    Task<AttachmentUploadRenewal> RenewUploadAsync(
        BitwardenAccountContext accountContext,
        RenewUploadRequest request,
        CancellationToken cancellationToken = default);

    Task UploadMultipartAsync(
        BitwardenAccountContext accountContext,
        UploadMultipartRequest request,
        CancellationToken cancellationToken = default);

    Task DownloadByTokenAsync(
        BitwardenAccountContext accountContext,
        DownloadByTokenRequest request,
        Func<Stream, Task> streamHandler,
        CancellationToken cancellationToken = default);
}
