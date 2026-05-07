using BitwardenApi.Modules.Notifications.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;

namespace FluentBitwarden.Modules.Session.Services;

internal sealed class SignalRAccessTokenProvider(IAccountSessionManager manager) : ISignalRAccessTokenProvider
{
    public async Task<string?> GetAccessToken()
    {
        var sessionTokens = await manager.GetValidActiveSessionTokensAsync(CancellationToken.None);
        return sessionTokens.AccessToken.ToString();
    }
}