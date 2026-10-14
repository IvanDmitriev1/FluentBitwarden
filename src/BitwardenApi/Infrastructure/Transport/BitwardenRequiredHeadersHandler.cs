using System.Net.Http.Headers;

namespace BitwardenApi.Infrastructure.Transport;

internal sealed class BitwardenRequiredHeadersHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        BitwardenRequiredHeaders.ApplyTo(request.Headers);
        return base.SendAsync(request, cancellationToken);
    }
}

internal static class BitwardenRequiredHeaders
{
    private const string ClientVersionHeaderName = "Bitwarden-Client-Version";
    private const string ClientVersion = "2026.2.1";

    public static void ApplyTo(HttpRequestHeaders headers)
    {
        headers.TryAddWithoutValidation(ClientVersionHeaderName, ClientVersion);
        headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}

