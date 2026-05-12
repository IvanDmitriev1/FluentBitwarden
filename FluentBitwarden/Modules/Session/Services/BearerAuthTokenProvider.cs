using System.Net.Http;
using System.Net.Http.Headers;
using FluentBitwarden.Modules.Session.Abstractions;

namespace FluentBitwarden.Modules.Session.Services;

[Fody.ConfigureAwait(false)]
internal sealed class BearerAuthTokenProvider(IAccountSessionManager manager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sessionTokens = await manager.GetValidActiveSessionTokensAsync(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionTokens.AccessToken.ToString());

        return await base.SendAsync(request, cancellationToken);
    }
}