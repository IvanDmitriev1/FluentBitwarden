using System.Text.Json.Serialization;
using FluentBitwarden.Modules.Passkey.Models;

namespace FluentBitwarden.Modules.Passkey.Internal;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(PasskeyGetAssertionRequest))]
[JsonSerializable(typeof(PasskeyAssertionResponse))]
internal sealed partial class PasskeyJsonContext : JsonSerializerContext
{
}
