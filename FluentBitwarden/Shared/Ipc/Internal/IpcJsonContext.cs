using FluentBitwarden.Shared.Ipc.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentBitwarden.Shared.Ipc.Internal;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(PingRequest))]
[JsonSerializable(typeof(PingResponse))]
internal sealed partial class IpcJsonContext : JsonSerializerContext
{
}