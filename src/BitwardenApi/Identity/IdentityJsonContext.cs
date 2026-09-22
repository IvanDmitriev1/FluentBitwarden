using System.Text.Json.Serialization;

namespace BitwardenApi.Identity;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(IdentityTokenAuthenticatedResponse))]
[JsonSerializable(typeof(IdentityTokenRefreshSessionResponse))]
[JsonSerializable(typeof(IdentityTokenFailureResponse))]
[JsonSerializable(typeof(WebAuthnAuthenticationAssertionOptionsResponse))]
[JsonSerializable(typeof(WebAuthnLoginAssertionResponseRequest))]
[JsonSerializable(typeof(PreloginRequest))]
[JsonSerializable(typeof(string))]
internal sealed partial class IdentityJsonContext : JsonSerializerContext
{
    public static IdentityJsonContext ConfiguredDefault { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new SessionAccessToken.SessionAccessTokenSystemTextJsonConverter());
        options.Converters.Add(new SessionRefreshToken.SessionRefreshTokenSystemTextJsonConverter());
        options.Converters.Add(new TwoFactorToken.TwoFactorTokenSystemTextJsonConverter());
        options.Converters.Add(new WebAuthnLoginAssertionOptionsToken.WebAuthnLoginAssertionOptionsTokenSystemTextJsonConverter());
        options.Converters.Add(new UserId.UserIdSystemTextJsonConverter());
        options.Converters.Add(new AuthRequestId.AuthRequestIdSystemTextJsonConverter());
        options.Converters.Add(new DeviceIdentifier.DeviceIdentifierSystemTextJsonConverter());
        options.Converters.Add(new DeviceName.DeviceNameSystemTextJsonConverter());
        return options;
    }
}
