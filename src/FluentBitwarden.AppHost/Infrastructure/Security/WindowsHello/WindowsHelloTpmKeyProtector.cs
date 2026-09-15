using System.Security.Cryptography;
using Windows.Security.Credentials;

namespace FluentBitwarden.AppHost.Infrastructure.Security.WindowsHello;

public static class WindowsHelloTpmKeyProtector
{
    /// <summary>
    /// Checks whether Windows Hello key credentials are available for the current Windows user.
    /// </summary>
    public static Task<bool> IsSupportedAsync() => KeyCredentialManager.IsSupportedAsync().AsTask();

    /// <summary>
    /// Creates or replaces the persisted Windows Hello RSA wrapping key used to protect the account user key.
    /// </summary>
    public static void CreateOrReplaceWrappingKey(string keyName, IntPtr ownerWindowHandle)
    {
        string persistentKeyName = WindowsHelloKeyName.Create(keyName);

        using var provider = WindowsHelloNcryptKeyStore.OpenProvider();
        using var key = WindowsHelloNcryptKeyStore.CreatePersistedKey(
            provider,
            persistentKeyName,
            overwrite: true);

        key.ConfigureNewWrappingKey(ownerWindowHandle);
        WindowsHelloNcryptKeyStore.FinalizeKey(key);
    }

    /// <summary>
    /// Deletes the persisted Windows Hello wrapping key when it exists.
    /// </summary>
    public static void DeleteWrappingKey(string keyName)
    {
        string persistentKeyName = WindowsHelloKeyName.Create(keyName);

        using var provider = WindowsHelloNcryptKeyStore.OpenProvider();
        using var key = WindowsHelloNcryptKeyStore.OpenKey(provider, persistentKeyName);

        WindowsHelloNcryptKeyStore.DeleteKey(key);
    }

    /// <summary>
    /// Best-effort deletes the persisted Windows Hello wrapping key without surfacing cleanup failures.
    /// </summary>
    public static bool TryDeleteWrappingKey(string keyName)
    {
        try
        {
            DeleteWrappingKey(keyName);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    /// <summary>
    /// Encrypts the decrypted Bitwarden user key with the persisted Windows Hello wrapping key.
    /// </summary>
    public static byte[] WrapUserKey(
        string keyName,
        ReadOnlySpan<byte> userKey,
        IntPtr ownerWindowHandle)
    {
        string persistentKeyName = WindowsHelloKeyName.Create(keyName);

        using var provider = WindowsHelloNcryptKeyStore.OpenProvider();
        using var key = WindowsHelloNcryptKeyStore.OpenKey(provider, persistentKeyName, silent: true);

        key.ApplyWindowHandle(ownerWindowHandle);

        return WindowsHelloNcryptKeyStore.Encrypt(key, userKey);
    }

    /// <summary>
    /// Prompts for Windows Hello and decrypts the wrapped Bitwarden user key.
    /// </summary>
    public static byte[] UnwrapUserKey(
        string keyName,
        ReadOnlySpan<byte> protectedUserKey,
        IntPtr ownerWindowHandle)
    {
        string persistentKeyName = WindowsHelloKeyName.Create(keyName);

        using var provider = WindowsHelloNcryptKeyStore.OpenProvider();
        using var key = WindowsHelloNcryptKeyStore.OpenKey(provider, persistentKeyName);

        key.ApplyUiContext(ownerWindowHandle);
        key.RequireGestureOnNextUse();

        return WindowsHelloNcryptKeyStore.Decrypt(key, protectedUserKey);
    }
}
