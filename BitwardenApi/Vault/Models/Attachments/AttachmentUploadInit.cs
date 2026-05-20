using System.Text.Json.Serialization;

namespace BitwardenApi.Models;

public sealed record AttachmentUploadInit
{
    [JsonPropertyName("attachmentId")]
    public AttachmentId AttachmentId { get; init; }

    [JsonPropertyName("url")]
    public Uri Url { get; init; } = null!;

    [JsonPropertyName("fileUploadType")]
    public AttachmentFileUploadType FileUploadType { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extra { get; init; } = [];
}
