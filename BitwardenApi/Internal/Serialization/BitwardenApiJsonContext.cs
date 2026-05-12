using System.Text.Json.Serialization;
using BitwardenApi.Internal.Identity;
using BitwardenApi.Internal.Notifications;
using BitwardenApi.Models;

namespace BitwardenApi.Internal.Serialization;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AttachmentUploadInit))]
[JsonSerializable(typeof(AttachmentUploadRenewal))]
[JsonSerializable(typeof(TokenAuthenticatedResponse))]
[JsonSerializable(typeof(TokenRefreshSessionResponse))]
[JsonSerializable(typeof(TokenFailureResponse))]
[JsonSerializable(typeof(WebAuthnLoginAssertionOptionsResponse))]
[JsonSerializable(typeof(WebAuthnLoginAssertionResponseRequest))]
[JsonSerializable(typeof(PreloginRequest))]
[JsonSerializable(typeof(NotificationEnvelope))]
[JsonSerializable(typeof(CipherChangedNotification))]
[JsonSerializable(typeof(FolderChangedNotification))]
[JsonSerializable(typeof(VaultSyncRequestedNotification))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Int64))]
internal sealed partial class BitwardenApiJsonContext : JsonSerializerContext
{
    public static BitwardenApiJsonContext ConfiguredDefault { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new AccessToken.AccessTokenSystemTextJsonConverter());
        options.Converters.Add(new RefreshToken.RefreshTokenSystemTextJsonConverter());
        options.Converters.Add(new TwoFactorToken.TwoFactorTokenSystemTextJsonConverter());
        options.Converters.Add(new EncryptedPrivateKey.EncryptedPrivateKeySystemTextJsonConverter());
        options.Converters.Add(new EncryptedUserKey.EncryptedUserKeySystemTextJsonConverter());
        options.Converters.Add(new WebAuthnLoginAssertionOptionsToken.WebAuthnLoginAssertionOptionsTokenSystemTextJsonConverter());
        options.Converters.Add(new UserId.UserIdSystemTextJsonConverter());
        options.Converters.Add(new AuthRequestId.AuthRequestIdSystemTextJsonConverter());
        options.Converters.Add(new DeviceIdentifier.DeviceIdentifierSystemTextJsonConverter());
        options.Converters.Add(new DeviceName.DeviceNameSystemTextJsonConverter());
        options.Converters.Add(new CipherId.CipherIdSystemTextJsonConverter());
        options.Converters.Add(new FolderId.FolderIdSystemTextJsonConverter());
        options.Converters.Add(new CollectionId.CollectionIdSystemTextJsonConverter());
        options.Converters.Add(new AttachmentId.AttachmentIdSystemTextJsonConverter());
        options.Converters.Add(new ContextId.ContextIdSystemTextJsonConverter());
        return options;
    }
}
