using System.Security.Cryptography;
using BitwaredApi.Abstractions;
using BitwaredApi.Abstractions.Exceptions;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Auth;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services.UnlockServices;

internal sealed class MasterPasswordUnlockService(
    IAuthService authService,
    ILocalVaultUnlocker localVaultUnlocker,
    LocalVaultUnlockStateRepository stateRepository,
    LocalVaultSessionUnlocker sessionUnlocker,
    SessionCoordinator sessionCoordinator,
    ICryptoService cryptoService) : IMasterPasswordUnlockService
{
    private const int AesNonceLength = 12;
    private const int AesTagLength = 16;

    public async ValueTask<AuthSession> UnlockAsync(
        StoredSessionInfo session,
        string masterPassword,
        CancellationToken cancellationToken = default)
    {
        bool isInitialized = await localVaultUnlocker.IsInitializedAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        if (!isInitialized)
        {
            AuthSession authSession = await authService.UnlockWithMasterPasswordAsync(masterPassword, cancellationToken).ConfigureAwait(false);
            byte[]? userKey = await authService.ExportUserKeyAsync(cancellationToken).ConfigureAwait(false);

            if (userKey is null || userKey.Length == 0)
            {
                throw new InvalidOperationException("The unlocked Bitwarden session did not expose a user key.");
            }

            try
            {
                await localVaultUnlocker.InitializeAsync(session.AccountId, userKey, cancellationToken).ConfigureAwait(false);
                await ConfigureMasterPasswordUnlockAsync(session, masterPassword, cancellationToken).ConfigureAwait(false);
                return authSession;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(userKey);
            }
        }

        LocalVaultUnlockerState state = await stateRepository.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        MasterPasswordLocalVaultKeyState masterPasswordState = state.MasterPassword
            ?? throw new InvalidOperationException("Master password local unlock is not configured for this session.");
        SessionState sessionStateForUnlock = await RequireStoredSessionStateAsync(session, cancellationToken).ConfigureAwait(false);

        byte[]? localVaultKey = null;

        try
        {
            localVaultKey = UnprotectLocalVaultKey(sessionStateForUnlock, masterPassword, masterPasswordState);
            return await sessionUnlocker.UnlockAsync(session.AccountId, localVaultKey, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (localVaultKey is not null)
            {
                CryptographicOperations.ZeroMemory(localVaultKey);
            }
        }
    }

    private async ValueTask ConfigureMasterPasswordUnlockAsync(
        StoredSessionInfo session,
        string masterPassword,
        CancellationToken cancellationToken)
    {
        byte[] localVaultKey = localVaultUnlocker.GetUnlockedLocalVaultKeyCopy();

        try
        {
            SessionState sessionState = await RequireStoredSessionStateAsync(session, cancellationToken).ConfigureAwait(false);
            LocalVaultUnlockerState state = await stateRepository.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);

            LocalVaultUnlockerState updatedState = state with
            {
                MasterPassword = ProtectLocalVaultKey(sessionState, masterPassword, localVaultKey),
            };

            await stateRepository.SaveAsync(updatedState, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(localVaultKey);
        }
    }

    private async ValueTask<SessionState> RequireStoredSessionStateAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken)
    {
        SessionState? state = await sessionCoordinator.GetStoredStateAsync(cancellationToken).ConfigureAwait(false);
        if (state is null || !string.Equals(state.AccountId, session.AccountId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("No persisted Bitwarden session state is available for this account.");
        }

        return state;
    }

    private MasterPasswordLocalVaultKeyState ProtectLocalVaultKey(
        SessionState sessionState,
        string masterPassword,
        byte[] localVaultKey)
    {
        byte[] derivedKey = DeriveProtectionKey(sessionState, masterPassword);
        byte[] nonce = RandomNumberGenerator.GetBytes(AesNonceLength);
        byte[] cipher = new byte[localVaultKey.Length];
        byte[] tag = new byte[AesTagLength];

        try
        {
            using AesGcm aes = new(derivedKey, AesTagLength);
            aes.Encrypt(nonce, localVaultKey, cipher, tag);

            return new MasterPasswordLocalVaultKeyState(
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(cipher),
                Convert.ToBase64String(tag));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(tag);
        }
    }

    private byte[] UnprotectLocalVaultKey(
        SessionState sessionState,
        string masterPassword,
        MasterPasswordLocalVaultKeyState state)
    {
        byte[] derivedKey = DeriveProtectionKey(sessionState, masterPassword);
        byte[] nonce = Convert.FromBase64String(state.Nonce);
        byte[] cipher = Convert.FromBase64String(state.Ciphertext);
        byte[] tag = Convert.FromBase64String(state.Tag);
        byte[] localVaultKey = new byte[cipher.Length];

        try
        {
            try
            {
                using AesGcm aes = new(derivedKey, AesTagLength);
                aes.Decrypt(nonce, cipher, tag, localVaultKey);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidCredentialsException("The supplied master password is incorrect.", ex);
            }

            return localVaultKey;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(localVaultKey);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(tag);
        }
    }

    private byte[] DeriveProtectionKey(SessionState sessionState, string masterPassword)
    {
        if (sessionState.KdfConfig is null)
        {
            throw new InvalidOperationException("This saved session cannot derive a master-password local unlock key.");
        }

        var auth = cryptoService.DeriveMasterPasswordAuth(
            sessionState.Email,
            masterPassword,
            sessionState.KdfConfig,
            sessionState.MasterPasswordSalt);

        try
        {
            return SHA256.HashData(auth.StretchedMasterKey);
        }
        finally
        {
            cryptoService.ZeroMemory(auth.MasterKey);
            cryptoService.ZeroMemory(auth.StretchedMasterKey);
        }
    }
}
