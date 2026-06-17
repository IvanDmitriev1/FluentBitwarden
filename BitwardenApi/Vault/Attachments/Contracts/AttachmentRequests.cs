namespace BitwardenApi.Vault.Attachments.Contracts;

/// <summary>
/// Starts attachment upload v2.
/// </summary>
/// <param name="AttachmentRequestJson">
/// JSON request stream. This stream is consumed and disposed by the API call.
/// </param>
public sealed record StartUploadV2Request(CipherId CipherId, Stream AttachmentRequestJson);

/// <summary>
/// Renews attachment upload.
/// </summary>
public sealed record RenewUploadRequest(CipherId CipherId, AttachmentId AttachmentId);

/// <summary>
/// Uploads attachment payload as multipart form data.
/// </summary>
/// <param name="File">
/// File stream content. This stream is consumed and disposed by the API call.
/// </param>
public sealed record UploadMultipartRequest(
    Uri RequestUri,
    Stream File,
    string FileName,
    string ContentType,
    IReadOnlyDictionary<string, string> FormFields);

public sealed record DownloadByTokenRequest(Uri RequestUri);

