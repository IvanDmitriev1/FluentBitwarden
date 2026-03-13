using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitwaredApi.Serialization;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PreloginRequestDto))]
[JsonSerializable(typeof(PreloginResponseDto))]
[JsonSerializable(typeof(TokenSuccessResponseDto))]
[JsonSerializable(typeof(TokenFailureResponseDto))]
[JsonSerializable(typeof(AuthRequestCreateRequestDto))]
[JsonSerializable(typeof(AuthRequestCreateResponseDto))]
[JsonSerializable(typeof(AuthRequestPollResponseDto))]
[JsonSerializable(typeof(JwtTokenPayloadDto))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(DateTimeOffset?))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, JsonElement>?>))]
internal sealed partial class BitwaredApiJsonContext : JsonSerializerContext
{
}
