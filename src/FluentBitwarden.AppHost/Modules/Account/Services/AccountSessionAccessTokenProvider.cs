using BitwardenApi.Client;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Account.Services;

internal sealed class AccountSessionAccessTokenProvider(
    IServiceScopeFactory scopeFactory,
    AccountTokenCache tokenCache) : IAccessTokenProvider
{
    public async ValueTask<SessionAccessToken> GetAccessTokenAsync(
        BitwardenAccountContext context,
        CancellationToken cancellationToken = default)
    {
        using var entry =
            await tokenCache.AcquireAsync(context, cancellationToken);

        if (entry.TryGetValidAccessToken(out var token))
            return token;

        await using var scope = scopeFactory.CreateAsyncScope();

        var accountService =
            scope.ServiceProvider.GetRequiredService<IAccountService>();

        var refreshed = await accountService.RefreshSessionTokens(
            context, cancellationToken);

        entry.Store(refreshed);
        return refreshed.AccessToken;
    }
}
