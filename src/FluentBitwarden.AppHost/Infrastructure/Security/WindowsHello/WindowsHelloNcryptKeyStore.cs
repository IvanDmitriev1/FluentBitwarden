using System.Security.Cryptography;
using Windows.Win32.Security.Cryptography;

namespace FluentBitwarden.AppHost.Infrastructure.Security.WindowsHello;

internal static class WindowsHelloNcryptKeyStore
{
    private const string PassportProviderName = "Microsoft Passport Key Storage Provider";
    private const string RsaAlgorithmName = "RSA";

    /// <summary>
    /// Opens the Windows Hello Passport key storage provider.
    /// </summary>
    public static NCryptFreeObjectSafeHandle OpenProvider()
    {
        WindowsHelloNcryptStatus.ThrowIfFailed(
            PInvoke.NCryptOpenStorageProvider(out NCryptFreeObjectSafeHandle provider, PassportProviderName, 0),
            nameof(PInvoke.NCryptOpenStorageProvider));

        return provider;
    }

    /// <summary>
    /// Creates a persisted Passport RSA key that can later wrap and unwrap the account user key.
    /// </summary>
    public static NCryptFreeObjectSafeHandle CreatePersistedKey(
        NCryptFreeObjectSafeHandle provider,
        string persistentKeyName,
        bool overwrite)
    {
        WindowsHelloNcryptStatus.ThrowIfFailed(
            PInvoke.NCryptCreatePersistedKey(
                provider,
                out NCryptFreeObjectSafeHandle key,
                RsaAlgorithmName,
                persistentKeyName,
                (CERT_KEY_SPEC)0,
                overwrite ? NCRYPT_FLAGS.NCRYPT_OVERWRITE_KEY_FLAG : 0),
            nameof(PInvoke.NCryptCreatePersistedKey));

        return key;
    }

    /// <summary>
    /// Opens an existing persisted Passport key, optionally suppressing UI during the open operation.
    /// </summary>
    public static NCryptFreeObjectSafeHandle OpenKey(
        NCryptFreeObjectSafeHandle provider,
        string persistentKeyName,
        bool silent = false)
    {
        WindowsHelloNcryptStatus.ThrowIfFailed(
            PInvoke.NCryptOpenKey(
                provider,
                out NCryptFreeObjectSafeHandle key,
                persistentKeyName,
                (CERT_KEY_SPEC)0,
                silent ? NCRYPT_FLAGS.NCRYPT_SILENT_FLAG : 0),
            nameof(PInvoke.NCryptOpenKey));

        return key;
    }

    /// <summary>
    /// Finalizes a newly created NCrypt key after all required properties have been applied.
    /// </summary>
    public static void FinalizeKey(NCryptFreeObjectSafeHandle key)
    {
        WindowsHelloNcryptStatus.ThrowIfFailed(
            PInvoke.NCryptFinalizeKey(key, 0),
            nameof(PInvoke.NCryptFinalizeKey));
    }

    /// <summary>
    /// Deletes a persisted Passport key and marks its safe handle as invalid because NCryptDeleteKey frees it.
    /// </summary>
    public static void DeleteKey(NCryptFreeObjectSafeHandle key)
    {
        int status = PInvoke.NCryptDeleteKey(key, 0);
        if (WindowsHelloNcryptStatus.Succeeded(status))
            key.SetHandleAsInvalid();

        WindowsHelloNcryptStatus.ThrowIfFailed(
            status,
            nameof(PInvoke.NCryptDeleteKey));
    }

    /// <summary>
    /// Encrypts the account user key with the public side of the Passport wrapping key.
    /// </summary>
    public static unsafe byte[] Encrypt(NCryptFreeObjectSafeHandle key, ReadOnlySpan<byte> plaintext)
    {
        WindowsHelloNcryptStatus.ThrowIfFailed(
            PInvoke.NCryptEncrypt(
                key,
                plaintext,
                null,
                Span<byte>.Empty,
                out uint resultLength,
                NCRYPT_FLAGS.NCRYPT_PAD_PKCS1_FLAG),
            nameof(PInvoke.NCryptEncrypt));

        if (resultLength > int.MaxValue)
            throw new CryptographicException("The Windows Hello wrapped key is too large.");

        byte[] ciphertext = GC.AllocateUninitializedArray<byte>((int)resultLength);
        WindowsHelloNcryptStatus.ThrowIfFailed(
            PInvoke.NCryptEncrypt(
                key,
                plaintext,
                null,
                ciphertext,
                out _,
                NCRYPT_FLAGS.NCRYPT_PAD_PKCS1_FLAG),
            nameof(PInvoke.NCryptEncrypt));

        return ciphertext;
    }

    /// <summary>
    /// Decrypts the wrapped account user key with the Passport private key after Windows Hello authorizes the use.
    /// </summary>
    public static unsafe byte[] Decrypt(NCryptFreeObjectSafeHandle key, ReadOnlySpan<byte> ciphertext)
    {
        byte[] plaintext = GC.AllocateUninitializedArray<byte>(ciphertext.Length);
        WindowsHelloNcryptStatus.ThrowIfFailed(
            PInvoke.NCryptDecrypt(
                key,
                ciphertext,
                null,
                plaintext,
                out uint resultLength,
                NCRYPT_FLAGS.NCRYPT_PAD_PKCS1_FLAG),
            nameof(PInvoke.NCryptDecrypt));

        if (resultLength > plaintext.Length)
            throw new CryptographicException("The Windows Hello unwrapped key is too large.");

        if (resultLength == plaintext.Length)
            return plaintext;

        byte[] resizedPlaintext = GC.AllocateUninitializedArray<byte>((int)resultLength);
        Buffer.BlockCopy(plaintext, 0, resizedPlaintext, 0, resizedPlaintext.Length);
        CryptographicOperations.ZeroMemory(plaintext);
        return resizedPlaintext;
    }
}
