using System.Text.Json;
using System.Text.Json.Serialization;
using FluentBitwarden.BrowserHost.Models;

namespace FluentBitwarden.BrowserHost.Infrastructure;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(BrowserCredentialAvailabilityPayload))]
[JsonSerializable(typeof(BrowserCredentialFillPayload))]
[JsonSerializable(typeof(BrowserVaultStatusResponse))]
[JsonSerializable(typeof(BrowserCredentialAvailabilityResponse))]
[JsonSerializable(typeof(BrowserCredentialListItem))]
[JsonSerializable(typeof(BrowserCredentialListItem[]))]
internal sealed partial class BrowserHostJsonContext : JsonSerializerContext;
