using BitwaredApi.Services;

namespace BitwaredApi.Http.DelegatingHandlers;

internal sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly SessionCoordinator _sessionCoordinator;

    public AuthHeaderHandler(SessionCoordinator sessionCoordinator)
    {
        _sessionCoordinator = sessionCoordinator;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Options.TryGetValue(HttpRequestOptionKeys.SkipAuthorization, out bool skipAuthorization)
            || !skipAuthorization)
        {
            string accessToken = await _sessionCoordinator.EnsureAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
