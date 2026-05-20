using System.Text.Json.Serialization;
using BitwardenApi.Models;
using BitwardenApi.Notifications.Infrastructure;

namespace BitwardenApi.Notifications.Serialization;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(NotificationEnvelope))]
[JsonSerializable(typeof(CipherChangedNotification))]
[JsonSerializable(typeof(FolderChangedNotification))]
[JsonSerializable(typeof(VaultSyncRequestedNotification))]
internal sealed partial class BitwardenNotificationsJsonContext : JsonSerializerContext
{
    public static BitwardenNotificationsJsonContext ConfiguredDefault { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new ContextId.ContextIdSystemTextJsonConverter());
        options.Converters.Add(new CipherId.CipherIdSystemTextJsonConverter());
        options.Converters.Add(new FolderId.FolderIdSystemTextJsonConverter());
        options.Converters.Add(new UserId.UserIdSystemTextJsonConverter());
        return options;
    }
}
