using System.Security.Cryptography;

namespace FluentBitwarden.Infrastructure.Security.Tmp;

public static class TpmDataProtector
{
    public const string PlatformCryptoProviderName = "Microsoft Platform Crypto Provider";
    private static readonly CngProvider TpmProvider = new(PlatformCryptoProviderName);

    private const int RsaKeySizeBits = 3072;
    private const string CngLengthPropertyName = "Length";

    public static void CreateOrReplaceKey(string keyName, TpmUiOptions uiOptions, IntPtr ownerWindowHandle)
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
                uiOptions.FriendlyName,
                uiOptions.Description,
                uiOptions.UseContext,
                uiOptions.CreationTitle)
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
        int maxPlaintextLength = data.Length / 8;

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