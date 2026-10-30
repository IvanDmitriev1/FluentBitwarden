using System.Text.Json.Serialization;

using BitwardenApi.Primitives;
using BitwardenApi.Vault.Attachments.Responses;
using BitwardenApi.Vault.Ciphers.Requests;
using BitwardenApi.Vault.Ciphers.Responses;
using BitwardenApi.Vault.Collections.Responses;
using BitwardenApi.Vault.Folders.Responses;
using BitwardenApi.Vault.Folders.Requests;
using BitwardenApi.Vault.Sync.Responses;
namespace BitwardenApi.Vault.Internal;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(VaultSyncResponse))]
[JsonSerializable(typeof(VaultProfileResponse))]
[JsonSerializable(typeof(VaultOrganizationResponse))]
[JsonSerializable(typeof(List<VaultOrganizationResponse>))]
[JsonSerializable(typeof(List<FolderResponse>))]
[JsonSerializable(typeof(List<CollectionResponse>))]
[JsonSerializable(typeof(CipherRequest))]
[JsonSerializable(typeof(CipherCreateRequest))]
[JsonSerializable(typeof(CipherBulkShareRequest))]
[JsonSerializable(typeof(CipherPartialRequest))]
[JsonSerializable(typeof(CipherShareRequest))]
[JsonSerializable(typeof(CipherBulkDeleteRequest))]
[JsonSerializable(typeof(CipherBulkMoveRequest))]
[JsonSerializable(typeof(List<CipherResponse>))]
[JsonSerializable(typeof(FolderRequest))]
[JsonSerializable(typeof(FolderBulkDeleteRequest))]
[JsonSerializable(typeof(AttachmentDownloadResponse))]
[JsonSerializable(typeof(List<AttachmentDownloadResponse>))]
[JsonSerializable(typeof(FileSize))]
[JsonSerializable(typeof(Int64))]
internal sealed partial class VaultJsonContext : JsonSerializerContext
{
    public static VaultJsonContext ConfiguredDefault { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new CipherId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new FolderId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new OrganizationId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new CollectionId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new AttachmentId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new UserId.NullSafeSystemTextJsonConverter());
        return options;
    }
}
