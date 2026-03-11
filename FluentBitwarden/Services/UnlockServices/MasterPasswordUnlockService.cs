using System.Security.Cryptography;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services.UnlockServices;

internal sealed class MasterPasswordUnlockService(
    ISessionManager sessionManager,
    ILocalVaultKeyManager localVaultKeyManager,
    ILocalVaultStateStore stateStore,
    IMasterPasswordUnlockWorkflow masterPasswordUnlockWorkflow) : IMasterPasswordUnlockService
{
    private const int AesNonceLength = 12;
    private const int AesTagLength = 16;

    public async ValueTask<SessionUnlockOutcome> UnlockAsync(
        StoredSessionInfo session,
        string masterPassword,
        CancellationToken cancellationToken = default)
    {
        bool isInitialized = await localVaultKeyManager.IsInitializedAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        PersistableSession sessionState = await RequireStoredSessionStateAsync(session, cancellationToken).ConfigureAwait(false);

        MasterPasswordUnlockOutcome workflowOutcome = await masterPasswordUnlockWorkflow
            .UnlockAsync(new MasterPasswordUnlockRequest(sessionState, masterPassword), cancellationToken)
            .ConfigureAwait(false);

        if (workflowOutcome is MasterPasswordUnlockOutcome.InvalidCredentials invalidCredentials)
        {
            return new SessionUnlockOutcome.InvalidCredentials(invalidCredentials.Message);
        }

        if (workflowOutcome is not MasterPasswordUnlockOutcome.Success success)
        {
            throw new InvalidOperationException("Unsupported master-password unlock outcome.");
        }

        byte[]? workflowUserKey = success.UserKey;
        byte[]? protectionKey = success.LocalVaultProtectionKey;

        try
        {
            if (!isInitialized)
            {
                SessionUnlockOutcome unlockOutcome = await sessionManager
                    .UnlockWithUserKeyAsync(workflowUserKey, cancellationToken)
                    .ConfigureAwait(false);

                if (unlockOutcome is not SessionUnlockOutcome.Success)
                {
                    return unlockOutcome;
                }

                await localVaultKeyManager.InitializeAsync(session.AccountId, workflowUserKey, cancellationToken).ConfigureAwait(false);
                await ConfigureMasterPasswordUnlockAsync(session, protectionKey, cancellationToken).ConfigureAwait(false);
                return unlockOutcome;
            }

            LocalVaultState state = await stateStore.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
            MasterPasswordLocalVaultKeyState masterPasswordState = state.MasterPassword
                ?? throw new InvalidOperationException("Master password local unlock is not configured for this session.");

            byte[]? localVaultKey = null;
            byte[]? userKey = null;

            try
            {
                if (!TryUnprotectLocalVaultKey(protectionKey, masterPasswordState, out localVaultKey))
                {
                    return new SessionUnlockOutcome.InvalidCredentials("The supplied master password is incorrect.");
                }

                byte[] unlockedLocalVaultKey = localVaultKey
                    ?? throw new InvalidOperationException("Local vault key is not available.");

                userKey = await localVaultKeyManager.DecryptUserKeyAsync(session.AccountId, unlockedLocalVaultKey, cancellationToken).ConfigureAwait(false);
                return await sessionManager.UnlockWithUserKeyAsync(userKey, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (userKey is not null)
                {
                    CryptographicOperations.ZeroMemory(userKey);
                }

                if (localVaultKey is not null)
                {
                    CryptographicOperations.ZeroMemory(localVaultKey);
                }
            }
        }
        finally
        {
            if (workflowUserKey is not null)
            {
                CryptographicOperations.ZeroMemory(workflowUserKey);
            }

            if (protectionKey is not null)
            {
                CryptographicOperations.ZeroMemory(protectionKey);
            }
        }
    }

    private async ValueTask ConfigureMasterPasswordUnlockAsync(
        StoredSessionInfo session,
        byte[] protectionKey,
        CancellationToken cancellationToken)
    {
        byte[] localVaultKey = localVaultKeyManager.GetUnlockedLocalVaultKeyCopy();

        try
        {
            LocalVaultState state = await stateStore.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
            LocalVaultState updatedState = state with
            {
                MasterPassword = ProtectLocalVaultKey(protectionKey, localVaultKey),
            };

            await stateStore.SaveAsync(updatedState, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(localVaultKey);
        }
    }

    private async ValueTask<PersistableSession> RequireStoredSessionStateAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken)
    {
        PersistableSession state = await sessionManager.RequirePersistedSessionAsync(cancellationToken).ConfigureAwait(false);
        if (!string.Equals(state.AccountId, session.AccountId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("No persisted Bitwarden session state is available for this account.");
        }

        return state;
    }

    private static MasterPasswordLocalVaultKeyState ProtectLocalVaultKey(
        byte[] protectionKey,
        byte[] localVaultKey)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(AesNonceLength);
        byte[] cipher = new byte[localVaultKey.Length];
        byte[] tag = new byte[AesTagLength];

        try
        {
            using AesGcm aes = new(protectionKey, AesTagLength);
            aes.Encrypt(nonce, localVaultKey, cipher, tag);

            return new MasterPasswordLocalVaultKeyState(
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(cipher),
                Convert.ToBase64String(tag));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(tag);
        }
    }

    private static bool TryUnprotectLocalVaultKey(
        byte[] protectionKey,
        MasterPasswordLocalVaultKeyState state,
        out byte[]? localVaultKey)
    {
        byte[] nonce = Convert.FromBase64String(state.Nonce);
        byte[] cipher = Convert.FromBase64String(state.Ciphertext);
        byte[] tag = Convert.FromBase64String(state.Tag);
        localVaultKey = new byte[cipher.Length];

        try
        {
            try
            {
                using AesGcm aes = new(protectionKey, AesTagLength);
                aes.Decrypt(nonce, cipher, tag, localVaultKey);
                return true;
            }
            catch (CryptographicException)
            {
                CryptographicOperations.ZeroMemory(localVaultKey);
                localVaultKey = null;
                return false;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(tag);
        }
    }
}
