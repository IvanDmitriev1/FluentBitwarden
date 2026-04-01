using BitwardenApi.Modules.Identity.Abstractions;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Modules.Session.Models.Exceptions;
using System.Collections.Concurrent;

namespace FluentBitwarden.Modules.Session.Services;

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
            if (sessionTokensStore.Get(userId) is not { } retrievedSession ||
                retrievedSession.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
                return current;

            var result = await identityApiClient.RefreshAsync(new RefreshLoginRequest(context, current.RefreshToken), ct);
            if (result is not TokenExchangeOutcome.Success success)
                throw new SessionRefreshException(result);

            var response = success.Response;
            var newSession = new SessionTokens(
                response.RefreshToken,
                response.TwoFactorToken,
                response.AccessToken,
                response.ExpiresAt);

            sessionTokensStore.Store(userId, newSession);
            currentSessionAccessor.UpdateSession(userId, newSession);
            return newSession;
        }
        finally
        {
            gate.Release();
        }
    }
}