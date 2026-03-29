using System.Text.Json;
using System.Text.Json.Serialization;
using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Default,
    Converters = [
        typeof(AccessToken.AccessTokenSystemTextJsonConverter), 
        typeof(RefreshToken.RefreshTokenSystemTextJsonConverter), 
        typeof(TwoFactorToken.TwoFactorTokenSystemTextJsonConverter)])]
[JsonSerializable(typeof(SessionTokens))]
[JsonSerializable(typeof(TpmProtectedSessionBlob))]
internal sealed partial class SessionJsonContext : JsonSerializerContext;
