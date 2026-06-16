using System.Text.Json;

namespace FluentBitwarden.BrowseProxy.Models;

internal sealed record BrowserNativeRequestEnvelope(
    int Version,
    string RequestId,
    ushort Type,
    JsonElement Payload);


public sealed record BrowserNativeResponseEnvelope<T>(
    string RequestId,
    T Payload);
