using FluentBitwarden.BrowserHost.Models;

namespace FluentBitwarden.BrowserHost.NativeMessaging;

internal interface INativeMessagingTransport
{
    Task<BrowserNativeRequestEnvelope?> ReadRequestAsync(CancellationToken cancellationToken);

    Task WriteResponseAsync<T>(
        string requestId,
        T payload,
        CancellationToken cancellationToken);
}
