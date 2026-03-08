using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Session;

namespace BitwaredApi.Services;

public sealed class SessionCoordinator
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly IClock _clock;
    private readonly ISessionStore _sessionStore;
    private readonly IEnvironmentConfig _environmentConfig;
    private readonly IDeviceInfoProvider _deviceInfoProvider;
    private readonly Http.IdentityClient _identityClient;
    private readonly ICryptoService _cryptoService;

    private bool _loaded;
    private SessionState? _state;
    private string? _accessToken;
    private byte[]? _userKey;

    public SessionCoordinator(
        IClock clock,
        ISessionStore sessionStore,
        IEnvironmentConfig environmentConfig,
        IDeviceInfoProvider deviceInfoProvider,
        Http.IdentityClient identityClient,
        ICryptoService cryptoService)
    {
        _clock = clock;
        _sessionStore = sessionStore;
        _environmentConfig = environmentConfig;
        _deviceInfoProvider = deviceInfoProvider;
        _identityClient = identityClient;
        _cryptoService = cryptoService;
    }

    public async ValueTask<SessionState?> GetStoredStateAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _state;
    }

    public bool HasUnlockedUserKey => _userKey is not null;

    public async ValueTask<string> EnsureAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        if (_state is null)
        {
            throw new InvalidOperationException("No persisted Bitwarden session is available.");
        }

        if (!string.IsNullOrWhiteSpace(_accessToken)
            && _state.AccessTokenExpiresAt - RefreshSkew > _clock.UtcNow)
        {
            return _accessToken;
        }

        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken)
                && _state.AccessTokenExpiresAt - RefreshSkew > _clock.UtcNow)
            {
                return _accessToken;
            }

            _environmentConfig.Set(new BitwardenEnvironment(new Uri(_state.ApiBase), new Uri(_state.IdentityBase)));

            string deviceIdentifier = await _deviceInfoProvider.GetDeviceIdentifierAsync(cancellationToken).ConfigureAwait(false);
            RefreshTokenRequestModel request = new(
                _state.RefreshToken,
                ClientType.Desktop,
                _deviceInfoProvider.DeviceType,
                _deviceInfoProvider.DeviceName,
                deviceIdentifier);

            TokenResponseModel token = await _identityClient.RefreshTokenAsync(request, cancellationToken).ConfigureAwait(false);

            _accessToken = token.AccessToken;
            _state = _state with
            {
                RefreshToken = token.RefreshToken ?? _state.RefreshToken,
                AccessTokenExpiresAt = token.ExpiresAt,
            };

            await _sessionStore.SaveAsync(_state, cancellationToken).ConfigureAwait(false);
            return _accessToken;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public async ValueTask SetSessionAsync(
        SessionState state,
        string accessToken,
        byte[]? userKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        _environmentConfig.Set(new BitwardenEnvironment(new Uri(state.ApiBase), new Uri(state.IdentityBase)));
        _state = state;
        _accessToken = accessToken;
        ReplaceUserKey(userKey);

        await _sessionStore.SaveAsync(state, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask RestoreUserKeyAsync(
        byte[] userKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userKey);

        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        if (_state is null)
        {
            throw new InvalidOperationException("No persisted Bitwarden session is available.");
        }

        _environmentConfig.Set(new BitwardenEnvironment(new Uri(_state.ApiBase), new Uri(_state.IdentityBase)));
        _accessToken = null;
        ReplaceUserKey(userKey);
    }

    public byte[]? GetUserKeyCopy()
    {
        if (_userKey is null)
        {
            return null;
        }

        byte[] copy = new byte[_userKey.Length];
        Buffer.BlockCopy(_userKey, 0, copy, 0, _userKey.Length);
        return copy;
    }

    public async ValueTask LockAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        _accessToken = null;
        ReplaceUserKey(null);
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        _accessToken = null;
        ReplaceUserKey(null);
        _state = null;
        await _sessionStore.ClearAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return;
        }

        await _loadGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_loaded)
            {
                return;
            }

            _state = await _sessionStore.LoadAsync(cancellationToken).ConfigureAwait(false);

            if (_state is not null)
            {
                _environmentConfig.Set(new BitwardenEnvironment(new Uri(_state.ApiBase), new Uri(_state.IdentityBase)));
            }

            _loaded = true;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private void ReplaceUserKey(byte[]? userKey)
    {
        _cryptoService.ZeroMemory(_userKey);
        _userKey = userKey is null ? null : userKey.ToArray();
    }
}
