using System.Text.Json.Serialization;

using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Primitives;
namespace BitwardenApi.Vault.Attachments.Responses;

public readonly struct AttachmentDownloadResponse
{
    [JsonPropertyName("id")]
    public required AttachmentId Id { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("fileName")]
    public required EncString EncryptedFileName { get; init; }

    [JsonPropertyName("key")]
    public required EncString ProtectedAttachmentKey { get; init; }

    [JsonPropertyName("size")]
    public required FileSize Size { get; init; }
}
