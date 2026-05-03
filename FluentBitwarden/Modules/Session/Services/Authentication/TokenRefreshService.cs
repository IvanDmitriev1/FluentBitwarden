using System.Collections.Concurrent;
using BitwardenApi.Modules.Identity.Abstractions;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Modules.Session.Models.Exceptions;
using System.Diagnostics;

namespace FluentBitwarden.Modules.Session.Services.Authentication;

[Fody.ConfigureAwait(false)]
internal sealed class TokenRefreshService(
    IIdentityApiClient identityApiClient,
    ISessionTokensStore sessionTokensStore,
    CurrentSessionAccessor currentSessionAccessor) : ITokenRefreshService
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<SessionTokens> RefreshAsync(UserId userId, BitwardenClientContext context, SessionTokens current, CancellationToken ct = default)
    {
        var gate = _locks.GetOrAdd(userId.Value, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);

        try
        {
            var refreshToken = current.RefreshToken;
            if (sessionTokensStore.Get(userId) is { } retrievedSession)
            {
                if (retrievedSession.AccessToken != AccessToken.Empty &&
                    retrievedSession.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    Debug.WriteLine("Using cached Bitwarden identity token from session store.");
                    currentSessionAccessor.UpdateSession(userId, retrievedSession);
                    return retrievedSession;
                }

                refreshToken = retrievedSession.RefreshToken;
            }

            Debug.WriteLine("Start refreshing Bitwarden identity token.");
            var result = await identityApiClient.RefreshAsync(new RefreshLoginRequest(context, refreshToken), ct);
            if (result is not TokenExchangeOutcome.SessionRefreshed success)
                throw new SessionRefreshException(result);

            var response = success.Session;
            var newSession = new SessionTokens(
                response.RefreshToken,
                response.TwoFactorToken,
                response.AccessToken,
                response.ExpiresAt);

            sessionTokensStore.Store(userId, newSession);
            currentSessionAccessor.UpdateSession(userId, newSession);
            Debug.WriteLine($"Finished refreshing Bitwarden identity token. Expires at {newSession.ExpiresAt:O}.");
            return newSession;
        }
        finally
        {
            gate.Release();
        }
    }
}
