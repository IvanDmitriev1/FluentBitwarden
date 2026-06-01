using System.Net.Http.Headers;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;

namespace FluentBitwarden.AppHost.Modules.Accounts.ApiAccess.Providers;

[Fody.ConfigureAwait(false)]
internal sealed class BearerAuthTokenProvider(
    IUnlockedAccountAccessor accountAccessor,
    IAccountAuthTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sessionTokens = await tokenProvider.GetValidTokensAsync(accountAccessor.CurrentAccount, cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionTokens.AccessToken.ToString());
        return await base.SendAsync(request, cancellationToken);
    }
}