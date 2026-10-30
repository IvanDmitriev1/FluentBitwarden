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
        options.Converters.Add(new ContextId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new CipherId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new FolderId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new UserId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new OrganizationId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new CollectionId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new AuthRequestId.NullSafeSystemTextJsonConverter());
        return options;
    }
}
