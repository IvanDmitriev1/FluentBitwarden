using BitwardenApi.Primitives;

namespace BitwardenApi.Attachments;

/// <summary>
/// Starts attachment upload v2.
/// </summary>
/// <param name="AttachmentRequestJson">
/// JSON request stream. This stream is consumed and disposed by the API call.
/// </param>
public sealed record StartUploadV2Request(
    BitwardenClientContext Context,
    AccessToken AccessToken,
    CipherId CipherId,
    Stream AttachmentRequestJson);

/// <summary>
/// Renews attachment upload.
/// </summary>
public sealed record RenewUploadRequest(
    BitwardenClientContext Context,
    AccessToken AccessToken,
    CipherId CipherId,
    AttachmentId AttachmentId);

/// <summary>
/// Uploads attachment payload as multipart form data.
/// </summary>
/// <param name="File">
/// File stream content. This stream is consumed and disposed by the API call.
/// </param>
public sealed record UploadMultipartRequest(
    BitwardenClientContext Context,
    Uri RequestUri,
    Stream File,
    string FileName,
    string ContentType,
    IReadOnlyDictionary<string, string>? FormFields = null,
    string FilePartName = "data");

public sealed record DownloadByTokenRequest(
    BitwardenClientContext Context,
    Uri RequestUri,
    AccessToken DownloadToken);
