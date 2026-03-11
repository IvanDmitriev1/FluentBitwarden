using System.Security.Cryptography;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services;

internal sealed class LocalVaultKeyManager(ILocalVaultStateStore stateStore)
    : ILocalVaultKeyManager
{
    private const int LocalVaultKeyLength = 32;
    private const int AesNonceLength = 12;
    private const int AesTagLength = 16;

    private readonly Lock _gate = new();
    private byte[]? _localVaultKey;

    public bool IsUnlocked
    {
        get
        {
            lock (_gate)
            {
                return _localVaultKey is not null;
            }
        }
    }

    public async ValueTask<bool> IsInitializedAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        return await stateStore.GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false) is not null;
    }

    public async ValueTask InitializeAsync(
        string accountId,
        byte[] userKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentNullException.ThrowIfNull(userKey);

        if (await IsInitializedAsync(accountId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        byte[] localVaultKey = RandomNumberGenerator.GetBytes(LocalVaultKeyLength);
        byte[] nonce = RandomNumberGenerator.GetBytes(AesNonceLength);
        byte[] cipher = new byte[userKey.Length];
        byte[] tag = new byte[AesTagLength];

        try
        {
            using AesGcm aes = new(localVaultKey, AesTagLength);
            aes.Encrypt(nonce, userKey, cipher, tag);

            LocalVaultState state = new(
                accountId,
                new EncryptedUserKeyPayload(
                    Convert.ToBase64String(nonce),
                    Convert.ToBase64String(cipher),
                    Convert.ToBase64String(tag)),
                null,
                null,
                null);

            await stateStore.SaveAsync(state, cancellationToken).ConfigureAwait(false);
            ReplaceLocalVaultKey(localVaultKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(localVaultKey);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(tag);
        }
    }

    public async ValueTask<byte[]> DecryptUserKeyAsync(
        string accountId,
        byte[] localVaultKey,
        CancellationToken cancellationToken = default)
    {
        LocalVaultState state = await stateStore.RequireForAccountAsync(accountId, cancellationToken).ConfigureAwait(false);
        EncryptedUserKeyPayload payload = state.Payload
            ?? throw new InvalidOperationException("The local vault state is missing its encrypted payload.");

        byte[] nonce = Convert.FromBase64String(payload.Nonce);
        byte[] cipher = Convert.FromBase64String(payload.Ciphertext);
        byte[] tag = Convert.FromBase64String(payload.Tag);
        byte[] userKey = new byte[cipher.Length];

        try
        {
            using AesGcm aes = new(localVaultKey, AesTagLength);
            aes.Decrypt(nonce, cipher, tag, userKey);
            ReplaceLocalVaultKey(localVaultKey);
            return userKey;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(userKey);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(tag);
        }
    }

    public byte[] GetUnlockedLocalVaultKeyCopy()
    {
        lock (_gate)
        {
            if (_localVaultKey is null)
            {
                throw new InvalidOperationException("Local vault is not unlocked.");
            }

            return _localVaultKey.ToArray();
        }
    }

    public ValueTask LockAsync(CancellationToken cancellationToken = default)
    {
        ReplaceLocalVaultKey(null);
        return ValueTask.CompletedTask;
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        ReplaceLocalVaultKey(null);
        await stateStore.ClearAsync(cancellationToken).ConfigureAwait(false);
    }

    private void ReplaceLocalVaultKey(byte[]? localVaultKey)
    {
        lock (_gate)
        {
            if (_localVaultKey is not null)
            {
                CryptographicOperations.ZeroMemory(_localVaultKey);
            }

            _localVaultKey = localVaultKey?.ToArray();
        }
    }
}
