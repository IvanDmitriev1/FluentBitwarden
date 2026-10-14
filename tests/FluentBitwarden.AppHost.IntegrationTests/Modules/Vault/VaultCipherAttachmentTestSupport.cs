using System.Security.Cryptography;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

internal static class VaultCipherAttachmentTestData
{
    internal static byte[] EncryptEncFile(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key)
    {
        using Aes aes = Aes.Create();
        aes.Key = key[..32].ToArray();
        aes.GenerateIV();
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] ciphertext = encryptor.TransformFinalBlock(plaintext.ToArray(), 0, plaintext.Length);

        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key[32..].ToArray());
        hmac.AppendData(aes.IV);
        hmac.AppendData(ciphertext);
        byte[] mac = hmac.GetHashAndReset();

        byte[] encryptedFile = new byte[1 + aes.IV.Length + mac.Length + ciphertext.Length];
        encryptedFile[0] = (byte)EncryptionType.AesCbc256_HmacSha256_B64;
        aes.IV.CopyTo(encryptedFile, 1);
        mac.CopyTo(encryptedFile, 1 + aes.IV.Length);
        ciphertext.CopyTo(encryptedFile, 1 + aes.IV.Length + mac.Length);
        return encryptedFile;
    }
}

internal sealed class TemporaryVaultAttachmentFile : IDisposable
{
    private readonly string _directoryPath = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "FluentBitwarden.AppHost.IntegrationTests",
        Guid.NewGuid().ToString("N"));

    public TemporaryVaultAttachmentFile()
    {
        Directory.CreateDirectory(_directoryPath);
        FilePath = System.IO.Path.Combine(_directoryPath, "attachment.bin");
    }

    public string FilePath { get; }

    public void Dispose()
    {
        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }
}
