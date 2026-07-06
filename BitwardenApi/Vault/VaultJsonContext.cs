using System.Text.Json.Serialization;
using BitwardenApi.Vault.Attachments.Contracts;

namespace BitwardenApi.Vault;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(VaultSyncResponse))]
[JsonSerializable(typeof(VaultProfileResponse))]
[JsonSerializable(typeof(VaultOrganizationResponse))]
[JsonSerializable(typeof(List<VaultOrganizationResponse>))]
[JsonSerializable(typeof(List<VaultFolderResponse>))]
[JsonSerializable(typeof(List<VaultCollectionResponse>))]
[JsonSerializable(typeof(List<VaultCipherResponse>))]
[JsonSerializable(typeof(VaultCipherRequest))]
[JsonSerializable(typeof(VaultCipherAttachmentDownloadResponse))]
[JsonSerializable(typeof(List<VaultCipherAttachmentDownloadResponse>))]
[JsonSerializable(typeof(FileSize))]
[JsonSerializable(typeof(Int64))]
internal sealed partial class VaultJsonContext : JsonSerializerContext
{
    public static VaultJsonContext ConfiguredDefault { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new CipherId.CipherIdSystemTextJsonConverter());
        options.Converters.Add(new FolderId.FolderIdSystemTextJsonConverter());
        options.Converters.Add(new OrganizationId.OrganizationIdSystemTextJsonConverter());
        options.Converters.Add(new CollectionId.CollectionIdSystemTextJsonConverter());
        options.Converters.Add(new AttachmentId.AttachmentIdSystemTextJsonConverter());
        options.Converters.Add(new UserId.UserIdSystemTextJsonConverter());
        return options;
    }
}
