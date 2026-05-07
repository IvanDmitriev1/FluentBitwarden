using System.Security.Cryptography;

namespace FluentBitwarden.Infrastructure.Security.Tmp;

public static class TpmCngDataProtector
{
    private static readonly CngProvider TpmProvider = CngProvider.MicrosoftPlatformCryptoProvider;

    private const int RsaKeySizeBits = 2048;
    private const string CngLengthPropertyName = "Length";

    public static bool IsSupported()
    {
        return false;
    }

    public static void CreateOrReplaceKey(string keyName, TpmCngUiOptions cngUiOptions, IntPtr ownerWindowHandle)
    {
        var creationParameters = new CngKeyCreationParameters
        {
            Provider = TpmProvider,
            KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey,
            KeyUsage = CngKeyUsages.Decryption,
            ExportPolicy = CngExportPolicies.None,
            ParentWindowHandle = ownerWindowHandle,
            UIPolicy = new CngUIPolicy(
                CngUIProtectionLevels.ForceHighProtection,
                cngUiOptions.FriendlyName,
                cngUiOptions.Description,
                cngUiOptions.UseContext,
                cngUiOptions.CreationTitle)
        };

        creationParameters.Parameters.Add(
            new CngProperty(
                CngLengthPropertyName,
                BitConverter.GetBytes(RsaKeySizeBits),
                CngPropertyOptions.None));

        using var cngKey = CngKey.Create(
            CngAlgorithm.Rsa,
            keyName,
            creationParameters);
    }

    public static bool KeyExists(string keyName) => CngKey.Exists(keyName, TpmProvider);

    public static byte[] ProtectData(string keyName, ReadOnlySpan<byte> data, IntPtr ownerWindowHandle)
    {
        using var key = OpenExistingKey(keyName, ownerWindowHandle);

        const int Sha256HashSizeBytes = 32;
        int keySizeBytes = key.KeySize / 8;
        int maxPlaintextLength = keySizeBytes - (2 * Sha256HashSizeBytes) - 2;

        if (data.Length > maxPlaintextLength)
        {
            throw new ArgumentException(
                $"Vault key is too large for RSA-OAEP-SHA256 wrapping. " +
                $"Key size: {key.KeySize} bits. " +
                $"Max plaintext: {maxPlaintextLength} bytes. " +
                $"Actual: {data.Length} bytes.",
                nameof(data));
        }

        using var rsa = new RSACng(key);
        byte[] wrapped = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        return wrapped;
    }


    public static byte[] UnprotectVaultKey(string keyName, ReadOnlySpan<byte> data, IntPtr ownerWindowHandle)
    {
        using var key = OpenExistingKey(keyName, ownerWindowHandle);
        using var rsa = new RSACng(key);

        return rsa.Decrypt(
            data,
            RSAEncryptionPadding.OaepSHA256);
    }

    private static CngKey OpenExistingKey(
        string keyName,
        IntPtr ownerWindowHandle)
    {
        if (!CngKey.Exists(keyName, TpmProvider))
            throw new CryptographicException(
                $"TPM key '{keyName}' was not found for the current Windows user.");

        CngKey key = CngKey.Open(keyName, TpmProvider);
        key.ParentWindowHandle = ownerWindowHandle;

        return key;
    }
}