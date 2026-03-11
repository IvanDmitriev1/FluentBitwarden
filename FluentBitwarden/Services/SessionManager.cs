using System.Security.Cryptography;
using BitwaredApi;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Services;

internal sealed class SessionManager(
    ISessionStore sessionStore,
    ILocalDeviceInfoProvider deviceInfoProvider,
    ISessionRefreshWorkflow sessionRefreshWorkflow,
    ILocalVaultKeyManager localVaultKeyManager)
    : ISessionManager
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Lock _runtimeSecretsGate = new();

    private bool _loaded;
    private PersistableSession? _state;
    private string? _accessToken;
    private byte[]? _userKey;

    public async ValueTask<StoredSessionInfo?> GetStoredSessionAsync(CancellationToken cancellationToken = default)
    {
        PersistableSession? state = await GetPersistedSessionAsync(cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            return null;
        }

        return new StoredSessionInfo(
            state.AccountId,
            state.Email,
            state.Environment,
            !HasUnlockedUserKey(),
            state.CanUnlockWithMasterPassword);
    }

    public async ValueTask CompleteAuthenticationAsync(
        AuthenticationSuccess authentication,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authentication);

        PersistableSession state = authentication.PersistableSession;
        if (string.IsNullOrWhiteSpace(authentication.AccessToken))
        {
            throw new InvalidOperationException("The authenticated session did not include an access token.");
        }

        byte[] userKey = authentication.DecryptedUserKey is { Length: > 0 }
            ? authentication.DecryptedUserKey
            : throw new InvalidOperationException("The authenticated session did not include a user key.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _state = state;
            _accessToken = authentication.AccessToken;
            ReplaceUserKey(userKey);
            _loaded = true;
            await sessionStore.SaveAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
            CryptographicOperations.ZeroMemory(userKey);
        }
    }

    public async ValueTask<PersistableSession> RequirePersistedSessionAsync(CancellationToken cancellationToken = default)
        => await GetPersistedSessionAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No persisted Bitwarden session is available.");

    public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => EnsureAccessTokenAsync(cancellationToken);

    public async ValueTask<SessionUnlockOutcome> UnlockWithUserKeyAsync(
        byte[] userKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userKey);

        PersistableSession? state = await GetPersistedSessionAsync(cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            return new SessionUnlockOutcome.Unavailable("No persisted Bitwarden session is available.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _accessToken = null;
            ReplaceUserKey(userKey);
            return new SessionUnlockOutcome.Success(CreateUnlockedSession(state));
        }
        finally
        {
            _gate.Release();
        }
    }

    public byte[]? GetUnlockedUserKeyCopy()
    {
        lock (_runtimeSecretsGate)
        {
            return _userKey?.ToArray();
        }
    }

    public async ValueTask LockAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _accessToken = null;
            ReplaceUserKey(null);
        }
        finally
        {
            _gate.Release();
        }

        await localVaultKeyManager.LockAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask LogoutAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _accessToken = null;
            ReplaceUserKey(null);
            _state = null;
            _loaded = true;
        }
        finally
        {
            _gate.Release();
        }

        await localVaultKeyManager.ClearAsync(cancellationToken).ConfigureAwait(false);
        await sessionStore.ClearAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<PersistableSession?> GetPersistedSessionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _state;
    }

    private async ValueTask<string> EnsureAccessTokenAsync(CancellationToken cancellationToken)
    {
        PersistableSession state = await RequirePersistedSessionAsync(cancellationToken).ConfigureAwait(false);

        if (HasFreshAccessToken(state))
        {
            return _accessToken!;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            PersistableSession persistedState = _state
                ?? throw new InvalidOperationException("No persisted Bitwarden session is available.");

            if (HasFreshAccessToken(persistedState))
            {
                return _accessToken!;
            }

            BitwardenDeviceInfo deviceInfo = deviceInfoProvider.DeviceInfo;
            SessionRefreshOutcome refreshOutcome = await sessionRefreshWorkflow
                .RefreshAsync(new SessionRefreshRequest(persistedState, deviceInfo), cancellationToken)
                .ConfigureAwait(false);

            if (refreshOutcome is not SessionRefreshOutcome.Success success)
            {
                throw new InvalidOperationException(
                    ((SessionRefreshOutcome.ReauthenticationRequired)refreshOutcome).Message);
            }

            _state = success.Session;
            _accessToken = success.AccessToken;
            await sessionStore.SaveAsync(_state, cancellationToken).ConfigureAwait(false);
            return _accessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_loaded)
            {
                return;
            }

            _state = await sessionStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            _loaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool HasUnlockedUserKey()
    {
        lock (_runtimeSecretsGate)
        {
            return _userKey is not null;
        }
    }

    private bool HasFreshAccessToken(PersistableSession state)
        => !string.IsNullOrWhiteSpace(_accessToken)
            && state.AccessTokenExpiresAt - RefreshSkew > DateTimeOffset.UtcNow;

    private void ReplaceUserKey(byte[]? userKey)
    {
        lock (_runtimeSecretsGate)
        {
            if (_userKey is not null)
            {
                CryptographicOperations.ZeroMemory(_userKey);
            }

            _userKey = userKey?.ToArray();
        }
    }

    private static AuthSession CreateUnlockedSession(PersistableSession state)
        => new(
            state.AccountId,
            state.Email,
            state.AccessTokenExpiresAt,
            state.Environment,
            true);
}
