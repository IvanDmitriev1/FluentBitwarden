using System.Text.Json;
using System.Text.Json.Serialization;
using FluentBitwarden.BrowserHost.Models;

namespace FluentBitwarden.BrowserHost.Infrastructure;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(BrowserNativeRequestEnvelope))]
[JsonSerializable(typeof(BrowserVaultStatusRequest))]
[JsonSerializable(typeof(BrowserVaultStatusResponse))]
[JsonSerializable(typeof(BrowserCredentialAvailabilityRequest))]
[JsonSerializable(typeof(BrowserCredentialAvailabilityResponse))]
[JsonSerializable(typeof(BrowserCredentialFillRequest))]
[JsonSerializable(typeof(BrowserCredentialFillResponse))]
[JsonSerializable(typeof(BrowserCredentialListItem))]
[JsonSerializable(typeof(BrowserCredentialListItem[]))]
internal sealed partial class BrowserHostJsonContext : JsonSerializerContext;
