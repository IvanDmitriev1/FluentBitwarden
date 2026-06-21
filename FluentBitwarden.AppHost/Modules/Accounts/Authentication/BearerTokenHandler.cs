using System.Net.Http.Headers;
using FluentBitwarden.AppHost.Application.Sessions;

namespace FluentBitwarden.AppHost.Modules.Accounts.Authentication;

[Fody.ConfigureAwait(false)]
internal sealed class BearerTokenHandler(
    IVaultSessionCoordinator vaultSessionCoordinator,
    IAccountTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var session = vaultSessionCoordinator.GetUnlockedSession();
        var sessionTokens = await tokenProvider.GetValidTokensAsync(session.Account, cancellationToken);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", sessionTokens.AccessToken.ToString());

        return await base.SendAsync(request, cancellationToken);
    }
}
