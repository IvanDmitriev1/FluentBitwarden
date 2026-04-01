using System.Text.Json;
using System.Text.Json.Serialization;
using FluentBitwarden.Modules.AppState.Models;

namespace FluentBitwarden.Modules.AppState.Internal;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(AppSettings))]
internal partial class AppStateJsonContext : JsonSerializerContext
{
}