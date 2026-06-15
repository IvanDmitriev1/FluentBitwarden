using FluentBitwarden.Contracts.Modules.Browser;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentBitwarden.BrowserHost.Dispatching;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(BrowserCredentialAvailabilityPayload))]
[JsonSerializable(typeof(BrowserCredentialFillPayload))]
[JsonSerializable(typeof(BrowserError))]
[JsonSerializable(typeof(BrowserRequestEnvelope))]
[JsonSerializable(typeof(BrowserResponseEnvelope))]
[JsonSerializable(typeof(BrowserVaultStatusResponse))]
[JsonSerializable(typeof(BrowserCredentialAvailabilityResponse))]
[JsonSerializable(typeof(BrowserCredentialFillResponse))]
[JsonSerializable(typeof(BrowserCredentialListItem))]
[JsonSerializable(typeof(BrowserCredentialListItem[]))]
internal sealed partial class BrowserHostJsonContext : JsonSerializerContext;
