using BitwaredApi;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using BitwaredApi.Services;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Services;

public sealed class SessionCoordinator(
    IClock clock,
    ISessionStore sessionStore,
    IEnvironmentConfig environmentConfig,
    IDeviceInfoProvider deviceInfoProvider,
    IIdentityClient identityClient,
    ICryptoService cryptoService)
    : IAccessTokenProvider
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _gate = new(1, 1);

    private bool _loaded;
    private SessionState? _state;
    private string? _accessToken;
    private byte[]? _userKey;

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
            && _state.AccessTokenExpiresAt - RefreshSkew > clock.UtcNow)
        {
            return _accessToken;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken)
                && _state.AccessTokenExpiresAt - RefreshSkew > clock.UtcNow)
            {
                return _accessToken;
            }

            environmentConfig.Set(new BitwardenEnvironment(new Uri(_state.ApiBase), new Uri(_state.IdentityBase)));

            string deviceIdentifier = await deviceInfoProvider.GetDeviceIdentifierAsync(cancellationToken).ConfigureAwait(false);
            RefreshTokenRequestModel request = new(
                _state.RefreshToken,
                ClientType.Desktop,
                deviceInfoProvider.DeviceType,
                deviceInfoProvider.DeviceName,
                deviceIdentifier);

            TokenResponseModel token = await identityClient.RefreshTokenAsync(request, cancellationToken).ConfigureAwait(false);

            _accessToken = token.AccessToken;
            _state = _state with
            {
                RefreshToken = token.RefreshToken ?? _state.RefreshToken,
                AccessTokenExpiresAt = token.ExpiresAt,
            };

            await sessionStore.SaveAsync(_state, cancellationToken).ConfigureAwait(false);
            return _accessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => EnsureAccessTokenAsync(cancellationToken);

    public async ValueTask SetSessionAsync(
        SessionState state,
        string accessToken,
        byte[]? userKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        environmentConfig.Set(new BitwardenEnvironment(new Uri(state.ApiBase), new Uri(state.IdentityBase)));
        _state = state;
        _accessToken = accessToken;
        ReplaceUserKey(userKey);

        await sessionStore.SaveAsync(state, cancellationToken).ConfigureAwait(false);
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

        environmentConfig.Set(new BitwardenEnvironment(new Uri(_state.ApiBase), new Uri(_state.IdentityBase)));
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
        await sessionStore.ClearAsync(cancellationToken).ConfigureAwait(false);
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

            if (_state is not null)
            {
                environmentConfig.Set(new BitwardenEnvironment(new Uri(_state.ApiBase), new Uri(_state.IdentityBase)));
            }

            _loaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ReplaceUserKey(byte[]? userKey)
    {
        cryptoService.ZeroMemory(_userKey);
        _userKey = userKey?.ToArray();
    }
}
