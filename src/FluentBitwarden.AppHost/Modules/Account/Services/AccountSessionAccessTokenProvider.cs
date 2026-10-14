using BitwardenApi.Infrastructure.Transport;
using BitwardenApi.Primitives;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Internal;

namespace FluentBitwarden.AppHost.Modules.Account.Services;

internal sealed class AccountSessionAccessTokenProvider(
    IAccountService accountService,
    AccountTokenCache tokenCache) : IBitwardenAccessTokenProvider
{
    public async ValueTask<SessionAccessToken> GetAccessTokenAsync(
        BitwardenAccountContext context,
        CancellationToken cancellationToken = default)
    {
        using var entry = await tokenCache.AcquireAsync(context, cancellationToken);

        if (entry.TryGetValidAccessToken(out SessionAccessToken token))
            return token;

        AccountSessionTokens refreshed = await accountService.RefreshSessionTokens(context, cancellationToken);

        entry.Store(refreshed);
        return refreshed.AccessToken;
    }
}
