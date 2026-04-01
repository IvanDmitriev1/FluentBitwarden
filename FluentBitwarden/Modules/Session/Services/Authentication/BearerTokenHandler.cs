using System.Net.Http;
using System.Net.Http.Headers;
using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Session.Abstractions;

namespace FluentBitwarden.Modules.Session.Services.Authentication;

[Fody.ConfigureAwait(false)]
internal sealed class BearerTokenHandler(
    ICurrentSessionAccessor currentSessionAccessor,
    ITokenRefreshService tokenRefreshService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!currentSessionAccessor.IsAuthenticated)
            throw new InvalidOperationException("Tried to request to authorized endpoint while unauthorized.");

        var currentSession = currentSessionAccessor.CurrentSession;
        var currentUser = currentSessionAccessor.CurrentUser;

        if (currentSession.AccessToken == AccessToken.Empty || currentSession.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            currentSession = await tokenRefreshService.RefreshAsync(
                currentUser,
                currentSessionAccessor.CurrentContext,
                currentSession,
                cancellationToken);
        }

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", currentSession.AccessToken.Value);

        return await base.SendAsync(request, cancellationToken);
    }
}