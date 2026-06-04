using System.Text.Json.Serialization;
using BitwardenApi.Models;

namespace BitwardenApi.Vault.Serialization;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(VaultSyncResponse))]
[JsonSerializable(typeof(VaultProfileDto))]
[JsonSerializable(typeof(VaultOrganizationDto))]
[JsonSerializable(typeof(List<VaultOrganizationDto>))]
[JsonSerializable(typeof(List<VaultFolderDto>))]
[JsonSerializable(typeof(List<VaultCollectionDto>))]
[JsonSerializable(typeof(List<VaultCipherDto>))]
[JsonSerializable(typeof(AttachmentUploadInit))]
[JsonSerializable(typeof(AttachmentUploadRenewal))]
[JsonSerializable(typeof(long))]
internal sealed partial class BitwardenVaultJsonContext : JsonSerializerContext
{
    public static BitwardenVaultJsonContext ConfiguredDefault { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new CipherId.CipherIdSystemTextJsonConverter());
        options.Converters.Add(new FolderId.FolderIdSystemTextJsonConverter());
        options.Converters.Add(new OrganizationId.OrganizationIdSystemTextJsonConverter());
        options.Converters.Add(new CollectionId.CollectionIdSystemTextJsonConverter());
        options.Converters.Add(new AttachmentId.AttachmentIdSystemTextJsonConverter());
        return options;
    }
}
