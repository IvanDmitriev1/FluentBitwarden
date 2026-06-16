using System.Text.Json.Serialization.Metadata;
using FluentBitwarden.BrowserHost.Models;

namespace FluentBitwarden.BrowserHost.NativeMessaging;

internal interface INativeMessagingTransport
{
    Task<BrowserNativeRequestEnvelope?> ReadRequestAsync(CancellationToken cancellationToken);

    Task WriteResponseAsync<T>(
        string requestId,
        T payload,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken);
}