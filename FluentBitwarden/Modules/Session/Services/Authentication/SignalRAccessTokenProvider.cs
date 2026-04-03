using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Notifications.Abstractions;
using FluentBitwarden.Modules.Session.Abstractions;

namespace FluentBitwarden.Modules.Session.Services.Authentication;

internal sealed class SignalRAccessTokenProvider(
    ICurrentSessionAccessor currentSessionAccessor,
    ITokenRefreshService tokenRefreshService) : ISignalRAccessTokenProvider
{
    public async Task<string?> GetAccessToken()
    {
        var currentSession = currentSessionAccessor.CurrentSession;
        var currentUser = currentSessionAccessor.CurrentUser;

        if (currentSession.AccessToken == AccessToken.Empty || currentSession.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            currentSession = await tokenRefreshService.RefreshAsync(
                currentUser,
                currentSessionAccessor.CurrentContext,
                currentSession);
        }

        return currentSession.AccessToken.ToString();
    }
}