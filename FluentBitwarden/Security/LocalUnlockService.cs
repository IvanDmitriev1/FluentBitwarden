using BitwaredApi.Abstractions.Exceptions;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Core.Abstractions;
using FluentBitwarden.Models;
using Microsoft.UI.Xaml;
using System.Security.Cryptography;
using System.Text.Json;
using Windows.Security.Credentials.UI;
using WinUIEx;

namespace FluentBitwarden.Security;

public sealed class LocalUnlockService(
    IAppPaths paths)
    : ILocalUnlockService
{
    private const int PinMinLength = 4;
    private const int PinMaxLength = 12;
    private const int PinIterations = 150_000;
    private const int AesNonceLength = 12;
    private const int AesTagLength = 16;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public async ValueTask<LocalUnlockStatus> GetStatusAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        LocalUnlockState? state = await LoadStateAsync(cancellationToken).ConfigureAwait(false);
        if (state is not null && !string.Equals(state.AccountId, accountId, StringComparison.Ordinal))
        {
            await ClearAsync(cancellationToken).ConfigureAwait(false);
            state = null;
        }

        bool isWindowsHelloAvailable = await IsWindowsHelloAvailableAsync().ConfigureAwait(false);

        return new LocalUnlockStatus(
            isWindowsHelloAvailable,
            state?.WindowsHello is not null,
            state?.Pin is not null);
    }

    public async ValueTask EnrollAsync(
        string accountId,
        byte[] userKey,
        UnlockEnrollmentSelection selection,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentNullException.ThrowIfNull(userKey);
        ArgumentNullException.ThrowIfNull(selection);

        string? pin = selection.Pin?.Trim();
        if (!selection.EnableWindowsHello && string.IsNullOrWhiteSpace(pin))
        {
            throw new InvalidOperationException("Select at least one local unlock method.");
        }

        if (!string.IsNullOrEmpty(pin))
        {
            ValidatePin(pin);
        }

        if (selection.EnableWindowsHello && !await IsWindowsHelloAvailableAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("Windows Hello is not available on this device.");
        }

        LocalUnlockState state = new(
            accountId,
            selection.EnableWindowsHello ? CreateWindowsHelloUnlock(userKey) : null,
            !string.IsNullOrEmpty(pin) ? CreatePinUnlock(pin, userKey) : null);

        await SaveStateAsync(state, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<byte[]> UnlockWithWindowsHelloAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        LocalUnlockState state = await RequireStateAsync(accountId, cancellationToken).ConfigureAwait(false);
        WindowsHelloUnlockState hello = state.WindowsHello
            ?? throw new InvalidOperationException("Windows Hello unlock is not enrolled for this session.");

        if (!await IsWindowsHelloAvailableAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("Windows Hello is not currently available.");
        }
        
        /*UserConsentVerificationResult result = await UserConsentVerifierInterop.RequestVerificationForWindowAsync( , "Verify with Windows Hello to unlock FluentBitwarden.");

        if (result != UserConsentVerificationResult.Verified)
        {
            throw new InvalidOperationException(GetWindowsHelloFailureMessage(result));
        }

        byte[] protectedBytes = Convert.FromBase64String(hello.ProtectedUserKey);
        try
        {
            return ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
        }*/

        return new byte[0];
    }

    public async ValueTask<byte[]> UnlockWithPinAsync(
        string accountId,
        string pin,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pin);

        ValidatePin(pin);

        LocalUnlockState state = await RequireStateAsync(accountId, cancellationToken).ConfigureAwait(false);
        PinUnlockState pinState = state.Pin
            ?? throw new InvalidOperationException("PIN unlock is not enrolled for this session.");

        byte[] salt = Convert.FromBase64String(pinState.Salt);
        byte[] nonce = Convert.FromBase64String(pinState.Nonce);
        byte[] cipher = Convert.FromBase64String(pinState.Ciphertext);
        byte[] tag = Convert.FromBase64String(pinState.Tag);
        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(pin, salt, pinState.Iterations, HashAlgorithmName.SHA256, 32);
        byte[] userKey = new byte[cipher.Length];

        try
        {
            try
            {
                using AesGcm aes = new(derivedKey, AesTagLength);
                aes.Decrypt(nonce, cipher, tag, userKey);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidCredentialsException("The supplied PIN is incorrect.", ex);
            }

            return userKey;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(userKey);
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

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(paths.UnlockStateFilePath))
        {
            File.Delete(paths.UnlockStateFilePath);
        }

        return ValueTask.CompletedTask;
    }

    private static void ValidatePin(string pin)
    {
        if (pin.Length < PinMinLength || pin.Length > PinMaxLength || pin.Any(ch => !char.IsDigit(ch)))
        {
            throw new InvalidOperationException($"PIN must be {PinMinLength}-{PinMaxLength} digits.");
        }
    }

    private async ValueTask<LocalUnlockState> RequireStateAsync(
        string accountId,
        CancellationToken cancellationToken)
    {
        LocalUnlockState? state = await LoadStateAsync(cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            throw new InvalidOperationException("No local unlock methods are enrolled for this session.");
        }

        if (!string.Equals(state.AccountId, accountId, StringComparison.Ordinal))
        {
            await ClearAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("Local unlock methods were configured for a different account.");
        }

        return state;
    }

    private static WindowsHelloUnlockState CreateWindowsHelloUnlock(byte[] userKey)
    {
        byte[] protectedBytes = ProtectedData.Protect(userKey, null, DataProtectionScope.CurrentUser);

        try
        {
            return new WindowsHelloUnlockState(Convert.ToBase64String(protectedBytes));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    private static PinUnlockState CreatePinUnlock(string pin, byte[] userKey)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] nonce = RandomNumberGenerator.GetBytes(AesNonceLength);
        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(pin, salt, PinIterations, HashAlgorithmName.SHA256, 32);
        byte[] cipher = new byte[userKey.Length];
        byte[] tag = new byte[AesTagLength];

        try
        {
            using AesGcm aes = new(derivedKey, AesTagLength);
            aes.Encrypt(nonce, userKey, cipher, tag);

            return new PinUnlockState(
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

    private async ValueTask<LocalUnlockState?> LoadStateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(paths.UnlockStateFilePath))
        {
            return null;
        }

        byte[] protectedBytes = await File.ReadAllBytesAsync(paths.UnlockStateFilePath, cancellationToken).ConfigureAwait(false);

        try
        {
            byte[] jsonBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);

            try
            {
                return JsonSerializer.Deserialize<LocalUnlockState>(jsonBytes, SerializerOptions);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(jsonBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    private async ValueTask SaveStateAsync(LocalUnlockState state, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(paths.UnlockStateFilePath)!);

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(state, SerializerOptions);

        try
        {
            byte[] protectedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);

            try
            {
                await File.WriteAllBytesAsync(paths.UnlockStateFilePath, protectedBytes, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(protectedBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(jsonBytes);
        }
    }

    private static async Task<bool> IsWindowsHelloAvailableAsync()
    {
        try
        {
            return await UserConsentVerifier.CheckAvailabilityAsync() == UserConsentVerifierAvailability.Available;
        }
        catch
        {
            return false;
        }
    }

    private static string GetWindowsHelloFailureMessage(UserConsentVerificationResult result)
        => result switch
        {
            UserConsentVerificationResult.Canceled => "Windows Hello verification was canceled.",
            UserConsentVerificationResult.DeviceBusy => "Windows Hello is busy. Try again.",
            UserConsentVerificationResult.DeviceNotPresent => "Windows Hello is not available on this device.",
            UserConsentVerificationResult.DisabledByPolicy => "Windows Hello is disabled by policy.",
            UserConsentVerificationResult.NotConfiguredForUser => "Windows Hello is not configured for the current user.",
            UserConsentVerificationResult.RetriesExhausted => "Windows Hello retries were exhausted.",
            _ => "Windows Hello verification failed.",
        };

    private sealed record LocalUnlockState(
        string AccountId,
        WindowsHelloUnlockState? WindowsHello,
        PinUnlockState? Pin);

    private sealed record WindowsHelloUnlockState(string ProtectedUserKey);

    private sealed record PinUnlockState(
        int Iterations,
        string Salt,
        string Nonce,
        string Ciphertext,
        string Tag);
}
