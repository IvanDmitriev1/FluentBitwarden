using System.Net.Http.Headers;

namespace BitwardenApi.Shared.Transport;

internal sealed class BitwardenRequiredHeadersHandler : DelegatingHandler
{
    private const string ClientVersionHeaderName = "Bitwarden-Client-Version";
    private const string ClientVersion = "2026.2.1";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(ClientVersionHeaderName, ClientVersion);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return base.SendAsync(request, cancellationToken);
    }
}
