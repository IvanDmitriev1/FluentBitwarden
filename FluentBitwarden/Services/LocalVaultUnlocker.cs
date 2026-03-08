using System.Security.Cryptography;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services;

internal sealed class LocalVaultUnlocker(LocalVaultUnlockStateRepository stateRepository)
    : ILocalVaultUnlocker
{
    private const int LocalVaultKeyLength = 32;
    private const int AesNonceLength = 12;
    private const int AesTagLength = 16;

    private readonly Lock _gate = new();

    private string? _accountId;
    private byte[]? _localVaultKey;

    public bool IsUnlocked => _localVaultKey is not null;

    public async ValueTask<bool> IsInitializedAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        return await stateRepository.GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false) is not null;
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

            LocalVaultUnlockerState state = new(
                accountId,
                new EncryptedLocalVaultPayload(
                    Convert.ToBase64String(nonce),
                    Convert.ToBase64String(cipher),
                    Convert.ToBase64String(tag)),
                null,
                null,
                null);

            await stateRepository.SaveAsync(state, cancellationToken).ConfigureAwait(false);
            ReplaceLocalVaultKey(accountId, localVaultKey);
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
        LocalVaultUnlockerState state = await stateRepository.RequireForAccountAsync(accountId, cancellationToken).ConfigureAwait(false);
        EncryptedLocalVaultPayload payload = state.Payload
            ?? throw new InvalidOperationException("The local vault unlocker is missing its encrypted payload.");

        byte[] nonce = Convert.FromBase64String(payload.Nonce);
        byte[] cipher = Convert.FromBase64String(payload.Ciphertext);
        byte[] tag = Convert.FromBase64String(payload.Tag);
        byte[] userKey = new byte[cipher.Length];

        try
        {
            using AesGcm aes = new(localVaultKey, AesTagLength);
            aes.Decrypt(nonce, cipher, tag, userKey);
            ReplaceLocalVaultKey(accountId, localVaultKey);
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
        if (_localVaultKey is null)
        {
            throw new InvalidOperationException("Local vault is not unlocked.");
        }

        lock (_gate)
        {
            byte[] copy = new byte[_localVaultKey.Length];
            Buffer.BlockCopy(_localVaultKey, 0, copy, 0, _localVaultKey.Length);
            return copy;
        }
    }

    public ValueTask LockAsync(CancellationToken cancellationToken = default)
    {
        ReplaceLocalVaultKey(null, null);
        return ValueTask.CompletedTask;
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        ReplaceLocalVaultKey(null, null);
        await stateRepository.ClearAsync(cancellationToken).ConfigureAwait(false);
    }

    private void ReplaceLocalVaultKey(string? accountId, byte[]? localVaultKey)
    {
        lock (_gate)
        {
            if (_localVaultKey is not null)
            {
                CryptographicOperations.ZeroMemory(_localVaultKey);
            }

            _accountId = accountId;
            _localVaultKey = localVaultKey?.ToArray();
        }
    }
}
