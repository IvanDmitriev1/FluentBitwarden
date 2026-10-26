using System.Net.Http.Headers;

namespace BitwardenApi.Client.Http;

internal sealed class AuthorizationHandler(
    IAccessTokenProvider accessTokenProvider) : DelegatingHandler
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
