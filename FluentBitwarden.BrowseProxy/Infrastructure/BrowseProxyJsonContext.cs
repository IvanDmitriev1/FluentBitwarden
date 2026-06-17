using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BitwardenApi.Primitives;
using FluentBitwarden.BrowseProxy.Models;

namespace FluentBitwarden.BrowseProxy.Infrastructure;

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
internal sealed partial class BrowseProxyJsonContext : JsonSerializerContext
{
    public static BrowseProxyJsonContext ConfiguredDefault { get; } = new(CreateOptions());

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new CipherId.CipherIdSystemTextJsonConverter());

        return options;
    }

    public JsonTypeInfo<T> GetRequiredTypeInfo<T>() =>
        GetTypeInfo(typeof(T)) as JsonTypeInfo<T> ??
        throw new InvalidOperationException($"Type '{typeof(T)}' is not configured for browse proxy JSON serialization.");
}
