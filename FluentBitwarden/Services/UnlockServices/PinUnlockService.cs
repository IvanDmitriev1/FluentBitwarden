using System.Security.Cryptography;
using BitwaredApi.Abstractions.Exceptions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Auth;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services.UnlockServices;

internal sealed class PinUnlockService(
    ILocalVaultUnlocker localVaultUnlocker,
    LocalVaultUnlockStateRepository stateRepository,
    LocalVaultSessionUnlocker sessionUnlocker)
    : IPinUnlockService
{
    private const int PinMinLength = 6;
    private const int PinMaxLength = 12;
    private const int PinIterations = 150_000;
    private const int AesNonceLength = 12;
    private const int AesTagLength = 16;

    public ValueTask<bool> IsConfiguredAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
        => stateRepository.HasPinEnrollmentAsync(session.AccountId, cancellationToken);

    public async ValueTask SetupAsync(
        StoredSessionInfo session,
        string pin,
        CancellationToken cancellationToken = default)
    {
        byte[]? localVaultKey = localVaultUnlocker.GetUnlockedLocalVaultKeyCopy();
        if (localVaultKey is null)
        {
            throw new InvalidOperationException("Unlock the local vault before setting up PIN unlock.");
        }

        try
        {
            LocalVaultUnlockerState state = await stateRepository.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
            LocalVaultUnlockerState updatedState = state with
            {
                Pin = ProtectLocalVaultKey(pin.Trim(), localVaultKey),
            };

            await stateRepository.SaveAsync(updatedState, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(localVaultKey);
        }
    }

    public async ValueTask DisableAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
    {
        LocalVaultUnlockerState state = await stateRepository.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        if (state.Pin is null)
        {
            return;
        }

        await stateRepository.SaveAsync(state with { Pin = null }, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<AuthSession> UnlockAsync(
        StoredSessionInfo session,
        string pin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(pin);

        LocalVaultUnlockerState state = await stateRepository.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        PinLocalVaultKeyState pinState = state.Pin
            ?? throw new InvalidOperationException("PIN unlock is not enrolled for this session.");

        byte[]? localVaultKey = null;

        try
        {
            localVaultKey = UnprotectLocalVaultKey(pinState, pin.Trim());
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

    private PinLocalVaultKeyState ProtectLocalVaultKey(string pin, byte[] localVaultKey)
    {
        ValidatePin(pin);

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] nonce = RandomNumberGenerator.GetBytes(AesNonceLength);
        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(pin, salt, PinIterations, HashAlgorithmName.SHA256, 32);
        byte[] cipher = new byte[localVaultKey.Length];
        byte[] tag = new byte[AesTagLength];

        try
        {
            using AesGcm aes = new(derivedKey, AesTagLength);
            aes.Encrypt(nonce, localVaultKey, cipher, tag);

            return new PinLocalVaultKeyState(
                PinIterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(cipher),
                Convert.ToBase64String(tag));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(derivedKey);
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(tag);
        }
    }

    private byte[] UnprotectLocalVaultKey(PinLocalVaultKeyState state, string pin)
    {
        ValidatePin(pin);

        byte[] salt = Convert.FromBase64String(state.Salt);
        byte[] nonce = Convert.FromBase64String(state.Nonce);
        byte[] cipher = Convert.FromBase64String(state.Ciphertext);
        byte[] tag = Convert.FromBase64String(state.Tag);
        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(pin, salt, state.Iterations, HashAlgorithmName.SHA256, 32);
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
                throw new InvalidCredentialsException("The supplied PIN is incorrect.", ex);
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
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(derivedKey);
        }
    }

    private static void ValidatePin(string pin)
    {
        if (pin.Length < PinMinLength || pin.Length > PinMaxLength || pin.Any(ch => !char.IsDigit(ch)))
        {
            throw new InvalidOperationException($"PIN must be {PinMinLength}-{PinMaxLength} digits.");
        }
    }
}
