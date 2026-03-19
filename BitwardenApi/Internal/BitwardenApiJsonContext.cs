using System.Text.Json;
using System.Text.Json.Serialization;
using BitwardenApi.Attachments;
using BitwardenApi.Identity;

namespace BitwardenApi.Internal;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(AttachmentUploadInit))]
[JsonSerializable(typeof(AttachmentUploadRenewal))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(string))]
internal sealed partial class BitwardenApiJsonContext : JsonSerializerContext;
