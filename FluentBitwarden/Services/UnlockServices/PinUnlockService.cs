using System.Security.Cryptography;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models.Session;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services.UnlockServices;

internal sealed class PinUnlockService(
    ILocalVaultKeyManager localVaultKeyManager,
    ILocalVaultStateStore stateStore,
    ISessionManager sessionManager)
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
        => stateStore.HasPinEnrollmentAsync(session.AccountId, cancellationToken);

    public async ValueTask<VaultConfigurationOutcome> SetupAsync(
        StoredSessionInfo session,
        string pin,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidatePin(pin, out string? validationError))
        {
            return new VaultConfigurationOutcome.InvalidInput(validationError!);
        }

        byte[] localVaultKey = localVaultKeyManager.GetUnlockedLocalVaultKeyCopy();

        try
        {
            LocalVaultState state = await stateStore.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
            LocalVaultState updatedState = state with
            {
                Pin = ProtectLocalVaultKey(pin.Trim(), localVaultKey),
            };

            await stateStore.SaveAsync(updatedState, cancellationToken).ConfigureAwait(false);
            return new VaultConfigurationOutcome.Success();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(localVaultKey);
        }
    }

    public async ValueTask<VaultConfigurationOutcome> DisableAsync(
        StoredSessionInfo session,
        CancellationToken cancellationToken = default)
    {
        LocalVaultState state = await stateStore.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        if (state.Pin is null)
        {
            return new VaultConfigurationOutcome.Success();
        }

        await stateStore.SaveAsync(state with { Pin = null }, cancellationToken).ConfigureAwait(false);
        return new VaultConfigurationOutcome.Success();
    }

    public async ValueTask<SessionUnlockOutcome> UnlockAsync(
        StoredSessionInfo session,
        string pin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(pin);

        LocalVaultState state = await stateStore.RequireForAccountAsync(session.AccountId, cancellationToken).ConfigureAwait(false);
        PinLocalVaultKeyState pinState = state.Pin
            ?? throw new InvalidOperationException("PIN unlock is not enrolled for this session.");

        byte[]? localVaultKey = null;
        byte[]? userKey = null;

        try
        {
            if (!TryUnprotectLocalVaultKey(pinState, pin.Trim(), out localVaultKey))
            {
                return new SessionUnlockOutcome.InvalidCredentials("The supplied PIN is incorrect.");
            }

            byte[] unlockedLocalVaultKey = localVaultKey
                ?? throw new InvalidOperationException("Local vault key is not available.");

            userKey = await localVaultKeyManager.DecryptUserKeyAsync(session.AccountId, unlockedLocalVaultKey, cancellationToken).ConfigureAwait(false);
            return await sessionManager.UnlockWithUserKeyAsync(userKey, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(userKey);
            if (localVaultKey is not null)
            {
                CryptographicOperations.ZeroMemory(localVaultKey);
            }
        }
    }

    private PinLocalVaultKeyState ProtectLocalVaultKey(string pin, byte[] localVaultKey)
    {
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

    private bool TryUnprotectLocalVaultKey(
        PinLocalVaultKeyState state,
        string pin,
        out byte[]? localVaultKey)
    {
        byte[] salt = Convert.FromBase64String(state.Salt);
        byte[] nonce = Convert.FromBase64String(state.Nonce);
        byte[] cipher = Convert.FromBase64String(state.Ciphertext);
        byte[] tag = Convert.FromBase64String(state.Tag);
        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(pin, salt, state.Iterations, HashAlgorithmName.SHA256, 32);
        localVaultKey = new byte[cipher.Length];

        try
        {
            try
            {
                using AesGcm aes = new(derivedKey, AesTagLength);
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
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(derivedKey);
        }
    }

    private static bool TryValidatePin(string pin, out string? errorMessage)
    {
        if (pin.Length < PinMinLength || pin.Length > PinMaxLength || pin.Any(ch => !char.IsDigit(ch)))
        {
            errorMessage = $"PIN must be {PinMinLength}-{PinMaxLength} digits.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
