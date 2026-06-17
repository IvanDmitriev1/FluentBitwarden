using System.Text.Json.Serialization;
using BitwardenApi.Primitives;
using BitwardenApi.Notifications;
using BitwardenApi.Notifications.Contracts;

namespace BitwardenApi.Notifications;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(NotificationEnvelope))]
[JsonSerializable(typeof(CipherChangedNotification))]
[JsonSerializable(typeof(FolderChangedNotification))]
[JsonSerializable(typeof(VaultSyncRequestedNotification))]
internal sealed partial class NotificationsJsonContext : JsonSerializerContext
{
    public static NotificationsJsonContext ConfiguredDefault { get; } = new(CreateOptions());

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
