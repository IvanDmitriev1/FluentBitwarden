using BitwardenApi.Infrastructure.Transport;
using BitwardenApi.Primitives;
using FluentBitwarden.AppHost.AppSession.Internal;
using FluentBitwarden.AppHost.Modules.Account.Contracts;

namespace FluentBitwarden.AppHost.AppSession.Services;

internal sealed class SessionAccessTokenProvider(
    IAccountService accountService,
    ActiveSessionManager activeSessionManager) : IBitwardenAccessTokenProvider
{
    public async ValueTask<SessionAccessToken> GetAccessTokenAsync(BitwardenAccountContext accountContext, CancellationToken cancellationToken = default)
    {
        if (activeSessionManager.TryGetSessionAccessToken(accountContext.UserId, out SessionAccessToken token))
            return token;

        using var transition = await activeSessionManager.EnterTransition(cancellationToken);
        if (activeSessionManager.TryGetSessionAccessToken(accountContext.UserId, out token))
            return token;

        var accountTokens = await accountService.RefreshSessionTokens(accountContext, cancellationToken);
        transition.UpdateSessionAccessToken(accountTokens);
        return accountTokens.AccessToken;
    }
}
