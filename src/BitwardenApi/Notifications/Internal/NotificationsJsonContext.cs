using System.Text.Json.Serialization;
using BitwardenApi.Notifications.Models;

namespace BitwardenApi.Notifications.Internal;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(NotificationEnvelope))]
[JsonSerializable(typeof(CipherChangedNotification))]
[JsonSerializable(typeof(FolderChangedNotification))]
[JsonSerializable(typeof(VaultSyncRequestedNotification))]
[JsonSerializable(typeof(SyncSendNotification))]
[JsonSerializable(typeof(AuthRequestPushNotification))]
[JsonSerializable(typeof(OrganizationStatusPushNotification))]
[JsonSerializable(typeof(OrganizationCollectionSettingChangedPushNotification))]
[JsonSerializable(typeof(OrganizationBankAccountVerifiedPushNotification))]
[JsonSerializable(typeof(ProviderBankAccountVerifiedPushNotification))]
[JsonSerializable(typeof(LogOutNotification))]
[JsonSerializable(typeof(AutoConfirmMemberNotification))]
[JsonSerializable(typeof(PremiumStatusChangedNotification))]
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
        options.Converters.Add(new OrganizationId.OrganizationIdSystemTextJsonConverter());
        options.Converters.Add(new CollectionId.CollectionIdSystemTextJsonConverter());
        options.Converters.Add(new AuthRequestId.AuthRequestIdSystemTextJsonConverter());
        return options;
    }
}
