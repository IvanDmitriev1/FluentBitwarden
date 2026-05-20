using System.Text.Json.Serialization;

namespace BitwardenApi.Infrastructure.Serialization;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(VaultSyncResponse))]
[JsonSerializable(typeof(List<VaultFolderDto>))]
[JsonSerializable(typeof(List<VaultCollectionDto>))]
[JsonSerializable(typeof(List<VaultCipherDto>))]
internal sealed partial class BitwardenVaultApiJsonContext : JsonSerializerContext
{
    public static BitwardenVaultApiJsonContext ConfiguredDefault { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new CipherId.CipherIdSystemTextJsonConverter());
        options.Converters.Add(new FolderId.FolderIdSystemTextJsonConverter());
        options.Converters.Add(new OrganizationId.OrganizationIdSystemTextJsonConverter());
        options.Converters.Add(new CollectionId.CollectionIdSystemTextJsonConverter());
        return options;
    }
}