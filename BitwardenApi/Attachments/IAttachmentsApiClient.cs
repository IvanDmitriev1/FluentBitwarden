using BitwardenApi.Internal;

namespace BitwardenApi.Attachments;

public interface IAttachmentsApiClient
{
    Task<AttachmentUploadInit> StartUploadV2Async(
        StartUploadV2Request request,
        CancellationToken cancellationToken = default);

    Task<AttachmentUploadRenewal> RenewUploadAsync(
        RenewUploadRequest request,
        CancellationToken cancellationToken = default);

    Task UploadMultipartAsync(
        UploadMultipartRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiStreamResponse> DownloadByTokenAsync(
        DownloadByTokenRequest request,
        CancellationToken cancellationToken = default);
}
