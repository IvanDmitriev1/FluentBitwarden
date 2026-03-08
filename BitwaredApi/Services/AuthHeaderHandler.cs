using BitwaredApi.Abstractions;

namespace BitwaredApi.Services;

internal sealed class AuthHeaderHandler(IAccessTokenProvider accessTokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Options.TryGetValue(HttpRequestOptionKeys.SkipAuthorization, out bool skipAuthorization)
            || !skipAuthorization)
        {
            string accessToken = await accessTokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
