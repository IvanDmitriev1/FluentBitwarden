using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitwardenApi.Attachments;

public sealed record AttachmentUploadRenewal
{
    [JsonPropertyName("url")]
    public Uri Url { get; init; } = null!;

    [JsonPropertyName("fileUploadType")]
    public AttachmentFileUploadType FileUploadType { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extra { get; init; } = [];
}
