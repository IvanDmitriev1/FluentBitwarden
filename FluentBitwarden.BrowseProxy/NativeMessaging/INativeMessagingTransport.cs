using FluentBitwarden.BrowseProxy.Models;

namespace FluentBitwarden.BrowseProxy.NativeMessaging;

internal interface INativeMessagingTransport
{
    Task<BrowserNativeRequestEnvelope?> ReadRequestAsync(CancellationToken cancellationToken);

    Task WriteResponseAsync<T>(
        string requestId,
        T payload,
        CancellationToken cancellationToken);
}
