using System.Security.Cryptography;
using System.Text;
using BitwaredApi;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Crypto.Enc;
using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;
using BitwaredApi.Services;
using BitwaredApi.Utilities;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Exceptions;
using FluentBitwarden.Models.Auth;
using FluentBitwarden.Models.Session;

namespace FluentBitwarden.Services;

public sealed class AuthService(
    IIdentityClient identityClient,
    IApiClient apiClient,
    ICryptoService cryptoService,
    IEnvironmentConfig environmentConfig,
    IDeviceInfoProvider deviceInfoProvider,
    SessionCoordinator sessionCoordinator,
    IClock clock)
    : IAuthService
{
    private readonly Lock _gate = new();

    private PendingPasswordLogin? _pendingPasswordLogin;
    private readonly Dictionary<string, PendingDeviceRequest> _pendingDeviceRequests = [];

    public async ValueTask<StoredSessionInfo?> GetStoredSessionAsync(CancellationToken cancellationToken = default)
    {
        SessionState? state = await sessionCoordinator.GetStoredStateAsync(cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            return null;
        }

        return new StoredSessionInfo(
            state.AccountId,
            state.Email,
            new BitwardenEnvironment(new Uri(state.ApiBase), new Uri(state.IdentityBase)),
            !sessionCoordinator.HasUnlockedUserKey,
            !string.IsNullOrWhiteSpace(state.MasterKeyEncryptedUserKey) && state.KdfConfig is not null);
    }

    public ValueTask<PreloginResponseModel> PreloginAsync(string email, CancellationToken cancellationToken = default)
        => identityClient.PreloginAsync(email, cancellationToken);

    public async ValueTask<AuthSession> SignInWithPasswordAsync(
        string email,
        string masterPassword,
        CancellationToken cancellationToken = default)
    {
        PreloginResponseModel prelogin = await identityClient.PreloginAsync(email, cancellationToken).ConfigureAwait(false);
        MasterPasswordAuth auth = cryptoService.DeriveMasterPasswordAuth(email, masterPassword, prelogin.Kdf);

        try
        {
            string deviceIdentifier = await deviceInfoProvider.GetDeviceIdentifierAsync(cancellationToken).ConfigureAwait(false);
            PasswordTokenRequestModel request = new(
                CryptoService.NormalizeEmail(email),
                auth.ServerAuthorizationHash,
                ClientType.Desktop,
                deviceInfoProvider.DeviceType,
                deviceInfoProvider.DeviceName,
                deviceIdentifier);

            TokenResponseModel token;

            try
            {
                token = await identityClient.ExchangePasswordAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (TokenEndpointException ex) when (ex.TwoFactorChallenge is not null)
            {
                SetPendingPasswordLogin(new PendingPasswordLogin(email, prelogin.Kdf, auth));
                throw new TwoFactorRequiredException(ex.TwoFactorChallenge);
            }

            return await CompletePasswordAuthAsync(email, prelogin.Kdf, auth, token, cancellationToken).ConfigureAwait(false);
        }
        catch (TwoFactorRequiredException)
        {
            throw;
        }
        catch
        {
            cryptoService.ZeroMemory(auth.MasterKey);
            cryptoService.ZeroMemory(auth.StretchedMasterKey);
            ClearPendingPasswordLogin();
            throw;
        }
    }

    public async ValueTask<AuthSession> ContinueTwoFactorAsync(
        string token,
        TwoFactorProviderType provider,
        bool remember,
        CancellationToken cancellationToken = default)
    {
        PendingPasswordLogin pending = GetPendingPasswordLogin();
        string deviceIdentifier = await deviceInfoProvider.GetDeviceIdentifierAsync(cancellationToken).ConfigureAwait(false);

        PasswordTokenRequestModel request = new(
            CryptoService.NormalizeEmail(pending.Email),
            pending.Auth.ServerAuthorizationHash,
            ClientType.Desktop,
            deviceInfoProvider.DeviceType,
            deviceInfoProvider.DeviceName,
            deviceIdentifier,
            token,
            provider,
            remember);

        TokenResponseModel response = await identityClient.ExchangePasswordAsync(request, cancellationToken).ConfigureAwait(false);
        AuthSession session = await CompletePasswordAuthAsync(
            pending.Email,
            pending.Kdf,
            pending.Auth,
            response,
            cancellationToken).ConfigureAwait(false);
        ClearPendingPasswordLogin();
        return session;
    }

    public async ValueTask<AuthSession> UnlockWithMasterPasswordAsync(
        string masterPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(masterPassword))
        {
            throw new InvalidCredentialsException("Enter your master password.");
        }

        SessionState state = await sessionCoordinator.GetStoredStateAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No persisted Bitwarden session is available.");

        if (state.KdfConfig is null || string.IsNullOrWhiteSpace(state.MasterKeyEncryptedUserKey))
        {
            throw new InvalidOperationException("This saved session cannot be unlocked with the master password.");
        }

        MasterPasswordAuth auth = cryptoService.DeriveMasterPasswordAuth(
            state.Email,
            masterPassword,
            state.KdfConfig,
            state.MasterPasswordSalt);

        byte[]? userKey = null;

        try
        {
            try
            {
                userKey = cryptoService.DecryptUserKey(
                    new EncString(state.MasterKeyEncryptedUserKey),
                    auth.StretchedMasterKey);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException or InvalidOperationException)
            {
                throw new InvalidCredentialsException("The supplied master password is incorrect.", ex);
            }

            await sessionCoordinator.RestoreUserKeyAsync(userKey, cancellationToken).ConfigureAwait(false);
            return CreateStoredAuthSession(state, true);
        }
        finally
        {
            cryptoService.ZeroMemory(userKey);
            cryptoService.ZeroMemory(auth.MasterKey);
            cryptoService.ZeroMemory(auth.StretchedMasterKey);
        }
    }

    public async ValueTask<AuthSession> UnlockWithUserKeyAsync(
        byte[] userKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userKey);

        SessionState state = await sessionCoordinator.GetStoredStateAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No persisted Bitwarden session is available.");

        await sessionCoordinator.RestoreUserKeyAsync(userKey, cancellationToken).ConfigureAwait(false);
        return CreateStoredAuthSession(state, true);
    }

    public async ValueTask<byte[]?> ExportUserKeyAsync(CancellationToken cancellationToken = default)
    {
        await sessionCoordinator.GetStoredStateAsync(cancellationToken).ConfigureAwait(false);
        return sessionCoordinator.GetUserKeyCopy();
    }

    public ValueTask<string> EnsureAccessTokenAsync(CancellationToken cancellationToken = default)
        => sessionCoordinator.EnsureAccessTokenAsync(cancellationToken);

    public async ValueTask<PendingDeviceLogin> StartDeviceLoginAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        string deviceIdentifier = await deviceInfoProvider.GetDeviceIdentifierAsync(cancellationToken).ConfigureAwait(false);
        string accessCode = GenerateAccessCode();

        using RSA rsa = RSA.Create(2048);
        byte[] privateKey = rsa.ExportPkcs8PrivateKey();
        byte[] publicKey = rsa.ExportSubjectPublicKeyInfo();
        string publicKeyB64 = Convert.ToBase64String(publicKey);
        string fingerprintPhrase = cryptoService.CreateFingerprintPhrase(email, publicKey);

        AuthRequestCreateResponse request = await apiClient.CreateAuthRequestAsync(
            CryptoService.NormalizeEmail(email),
            deviceIdentifier,
            publicKeyB64,
            AuthRequestType.AuthenticateAndUnlock,
            accessCode,
            cancellationToken).ConfigureAwait(false);

        PendingDeviceLogin pending = new(
            request.Id,
            request.AccessCode,
            fingerprintPhrase,
            request.Expires,
            CryptoService.NormalizeEmail(email));

        lock (_gate)
        {
            _pendingDeviceRequests[request.Id] = new PendingDeviceRequest(email, request.AccessCode, privateKey);
        }

        Array.Clear(publicKey, 0, publicKey.Length);
        return pending;
    }

    public async ValueTask<AuthSession> WaitForDeviceApprovalAsync(
        PendingDeviceLogin pendingRequest,
        CancellationToken cancellationToken = default)
    {
        PendingDeviceRequest pending = GetPendingDeviceRequest(pendingRequest.RequestId);

        try
        {
            while (true)
            {
                AuthRequestStatusResponse status = await apiClient.GetAuthResponseAsync(
                    pendingRequest.RequestId,
                    pendingRequest.AccessCode,
                    cancellationToken).ConfigureAwait(false);

                if (status.Expired || pendingRequest.Expires <= clock.UtcNow)
                {
                    throw new DeviceApprovalPendingException("The device login request expired before approval.");
                }

                if (!status.Answered)
                {
                    await clock.DelayAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (!status.Approved || string.IsNullOrWhiteSpace(status.EncryptedUserKey))
                {
                    throw new InvalidCredentialsException("The device login request was denied.");
                }

                byte[] userKey = cryptoService.DecryptRsaWrappedKey(new EncString(status.EncryptedUserKey), pending.PrivateKeyPkcs8);

                try
                {
                    string deviceIdentifier = await deviceInfoProvider.GetDeviceIdentifierAsync(cancellationToken).ConfigureAwait(false);
                    PasswordTokenRequestModel tokenRequest = new(
                        pendingRequest.Email,
                        pending.AccessCode,
                        ClientType.Desktop,
                        deviceInfoProvider.DeviceType,
                        deviceInfoProvider.DeviceName,
                        deviceIdentifier,
                        AuthRequestId: pendingRequest.RequestId);

                    TokenResponseModel response = await identityClient.ExchangePasswordAsync(tokenRequest, cancellationToken).ConfigureAwait(false);
                    return await CompleteAuthAsync(pendingRequest.Email, null, null, response, userKey, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    cryptoService.ZeroMemory(userKey);
                }
            }
        }
        finally
        {
            ClearPendingDeviceRequest(pendingRequest.RequestId);
        }
    }

    public async ValueTask LockAsync(CancellationToken cancellationToken = default)
    {
        ClearPendingPasswordLogin();
        ClearAllPendingDeviceRequests();
        await sessionCoordinator.LockAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask LogoutAsync(CancellationToken cancellationToken = default)
    {
        ClearPendingPasswordLogin();
        ClearAllPendingDeviceRequests();
        await sessionCoordinator.ClearAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<AuthSession> CompletePasswordAuthAsync(
        string email,
        KdfConfigModel kdf,
        MasterPasswordAuth auth,
        TokenResponseModel response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await CompleteAuthAsync(email, kdf, auth, response, null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            cryptoService.ZeroMemory(auth.MasterKey);
            cryptoService.ZeroMemory(auth.StretchedMasterKey);
        }
    }

    private async ValueTask<AuthSession> CompleteAuthAsync(
        string email,
        KdfConfigModel? kdf,
        MasterPasswordAuth? auth,
        TokenResponseModel response,
        byte[]? deviceLoginUserKey,
        CancellationToken cancellationToken)
    {
        string accountId = JwtTokenReader.GetClaim(response.AccessToken, "sub")
            ?? JwtTokenReader.GetClaim(response.AccessToken, "nameid")
            ?? CryptoService.NormalizeEmail(email);

        byte[]? userKey = null;
        string? masterKeyEncryptedUserKey = response.UserDecryptionOptions?.MasterPasswordUnlock?.MasterKeyEncryptedUserKey
            ?? response.Key;
        string? salt = response.UserDecryptionOptions?.MasterPasswordUnlock?.Salt
            ?? (kdf is not null ? CryptoService.NormalizeEmail(email) : null);
        KdfConfigModel? persistedKdf = response.UserDecryptionOptions?.MasterPasswordUnlock?.Kdf
            ?? response.Kdf
            ?? kdf;

        if (deviceLoginUserKey is not null)
        {
            userKey = deviceLoginUserKey.ToArray();
        }
        else if (auth is not null && string.IsNullOrWhiteSpace(masterKeyEncryptedUserKey))
        {
            throw new ServerVersionMismatchException("Identity response did not include the encrypted user key required for master-password unlock.");
        }
        else if (auth is not null && !string.IsNullOrWhiteSpace(masterKeyEncryptedUserKey))
        {
            userKey = cryptoService.DecryptUserKey(new EncString(masterKeyEncryptedUserKey), auth.StretchedMasterKey);
        }

        string refreshToken = response.RefreshToken ?? throw new ServerVersionMismatchException("Identity response did not include refresh_token.");
        string deviceIdentifier = await deviceInfoProvider.GetDeviceIdentifierAsync(cancellationToken).ConfigureAwait(false);

        SessionState state = new(
            accountId,
            CryptoService.NormalizeEmail(email),
            environmentConfig.Current.ApiBase.AbsoluteUri,
            environmentConfig.Current.IdentityBase.AbsoluteUri,
            refreshToken,
            response.ExpiresAt,
            ClientType.Desktop.ToClientId(),
            deviceIdentifier,
            masterKeyEncryptedUserKey,
            response.PrivateKey,
            salt,
            persistedKdf);

        try
        {
            await sessionCoordinator.SetSessionAsync(state, response.AccessToken, userKey, cancellationToken).ConfigureAwait(false);

            return new AuthSession(
                accountId,
                CryptoService.NormalizeEmail(email),
                response.ExpiresAt,
                environmentConfig.Current,
                userKey is not null);
        }
        finally
        {
            cryptoService.ZeroMemory(userKey);
        }
    }

    private PendingPasswordLogin GetPendingPasswordLogin()
    {
        lock (_gate)
        {
            return _pendingPasswordLogin
                ?? throw new InvalidOperationException("No pending two-factor challenge is available.");
        }
    }

    private void SetPendingPasswordLogin(PendingPasswordLogin pending)
    {
        lock (_gate)
        {
            ClearPendingPasswordLoginInternal();
            _pendingPasswordLogin = pending;
        }
    }

    private void ClearPendingPasswordLogin()
    {
        lock (_gate)
        {
            ClearPendingPasswordLoginInternal();
        }
    }

    private void ClearPendingPasswordLoginInternal()
    {
        if (_pendingPasswordLogin is null)
            return;

        cryptoService.ZeroMemory(_pendingPasswordLogin.Auth.MasterKey);
        cryptoService.ZeroMemory(_pendingPasswordLogin.Auth.StretchedMasterKey);
        _pendingPasswordLogin = null;
    }

    private PendingDeviceRequest GetPendingDeviceRequest(string requestId)
    {
        lock (_gate)
        {
            return _pendingDeviceRequests.TryGetValue(requestId, out PendingDeviceRequest? pending)
                ? pending
                : throw new InvalidOperationException("The device login request is no longer available in memory.");
        }
    }

    private void ClearPendingDeviceRequest(string requestId)
    {
        lock (_gate)
        {
            if (_pendingDeviceRequests.Remove(requestId, out PendingDeviceRequest? pending))
            {
                cryptoService.ZeroMemory(pending.PrivateKeyPkcs8);
            }
        }
    }

    private void ClearAllPendingDeviceRequests()
    {
        lock (_gate)
        {
            foreach (PendingDeviceRequest pending in _pendingDeviceRequests.Values)
            {
                cryptoService.ZeroMemory(pending.PrivateKeyPkcs8);
            }

            _pendingDeviceRequests.Clear();
        }
    }

    private static string GenerateAccessCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        Span<byte> randomBytes = stackalloc byte[25];
        RandomNumberGenerator.Fill(randomBytes);
        StringBuilder builder = new(randomBytes.Length);

        foreach (byte randomByte in randomBytes)
        {
            builder.Append(alphabet[randomByte % alphabet.Length]);
        }

        return builder.ToString();
    }

    private static AuthSession CreateStoredAuthSession(SessionState state, bool hasUserKey)
        => new(
            state.AccountId,
            state.Email,
            state.AccessTokenExpiresAt,
            new BitwardenEnvironment(new Uri(state.ApiBase), new Uri(state.IdentityBase)),
            hasUserKey);

    private sealed record PendingPasswordLogin(
        string Email,
        KdfConfigModel Kdf,
        MasterPasswordAuth Auth);

    private sealed record PendingDeviceRequest(
        string Email,
        string AccessCode,
        byte[] PrivateKeyPkcs8);
}
