using System.Net.Http.Headers;

namespace BitwardenApi.Infrastructure.Transport;

internal sealed class BitwardenAuthorizationHandler(
    IBitwardenAccessTokenProvider accessTokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        BitwardenAccountContext accountContext = request.GetBitwardenAccountContext();
        SessionAccessToken sessionAccessToken = await accessTokenProvider.GetAccessTokenAsync(
            accountContext,
            cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            sessionAccessToken.ToString());

        return await base.SendAsync(request, cancellationToken);
    }
}
