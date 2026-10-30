using System.Text.Json.Serialization;

using BitwardenApi.Identity.Models;
namespace BitwardenApi.Identity.Internal;

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
        options.Converters.Add(new SessionAccessToken.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new SessionRefreshToken.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new TwoFactorToken.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new WebAuthnLoginAssertionOptionsToken.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new UserId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new AuthRequestId.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new DeviceIdentifier.NullSafeSystemTextJsonConverter());
        options.Converters.Add(new DeviceName.NullSafeSystemTextJsonConverter());
        return options;
    }
}
