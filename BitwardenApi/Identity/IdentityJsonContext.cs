using System.Text.Json.Serialization;
using BitwardenApi.Identity.Contracts;
using BitwardenApi.Primitives;

namespace BitwardenApi.Identity;

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
internal sealed partial class IdentityJsonContext : JsonSerializerContext
{
    public static IdentityJsonContext ConfiguredDefault { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new AccessToken.AccessTokenSystemTextJsonConverter());
        options.Converters.Add(new RefreshToken.RefreshTokenSystemTextJsonConverter());
        options.Converters.Add(new TwoFactorToken.TwoFactorTokenSystemTextJsonConverter());
        options.Converters.Add(new WebAuthnLoginAssertionOptionsToken.WebAuthnLoginAssertionOptionsTokenSystemTextJsonConverter());
        options.Converters.Add(new UserId.UserIdSystemTextJsonConverter());
        options.Converters.Add(new AuthRequestId.AuthRequestIdSystemTextJsonConverter());
        options.Converters.Add(new DeviceIdentifier.DeviceIdentifierSystemTextJsonConverter());
        options.Converters.Add(new DeviceName.DeviceNameSystemTextJsonConverter());
        return options;
    }
}
