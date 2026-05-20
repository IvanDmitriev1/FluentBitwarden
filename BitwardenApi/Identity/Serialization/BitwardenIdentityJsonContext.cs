using System.Text.Json.Serialization;
using BitwardenApi.Identity.Infrastructure.Payloads;
using BitwardenApi.Models;

namespace BitwardenApi.Identity.Serialization;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(TokenAuthenticatedResponse))]
[JsonSerializable(typeof(TokenRefreshSessionResponse))]
[JsonSerializable(typeof(TokenFailureResponse))]
[JsonSerializable(typeof(WebAuthnLoginAssertionOptionsResponse))]
[JsonSerializable(typeof(WebAuthnLoginAssertionResponseRequest))]
[JsonSerializable(typeof(PreloginRequest))]
[JsonSerializable(typeof(string))]
internal sealed partial class BitwardenIdentityJsonContext : JsonSerializerContext
{
    public static BitwardenIdentityJsonContext ConfiguredDefault { get; } = new(CreateOptions());

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
        return options;
    }
}
