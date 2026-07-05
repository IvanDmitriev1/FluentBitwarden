using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Infrastructure.Cryptography.Enc;

/// <summary>
/// An encrypted Bitwarden attachment file: the encrypted source stream together with the
/// <see cref="AttachmentKey"/> that decrypts it. Owns the wire-format parsing and streaming decrypt.
/// </summary>
public static class EncFile
{
    private const int TypeByteLength = 1;
    private const int IvByteLength = 16;
    private const int MacByteLength = 32;
    private const int HeaderByteLength = TypeByteLength + IvByteLength + MacByteLength;
    private const int CopyBufferByteLength = 40960;

    /// <summary>
    /// Decrypts the file into <paramref name="plaintextStream"/>. The wire format is a fixed header (type byte
    /// + 16-byte IV + 32-byte MAC) followed by the AES-CBC ciphertext. The ciphertext is spooled to a
    /// temp file while its HMAC is computed over IV + ciphertext; the MAC is verified before any
    /// plaintextStream is produced, so a tampered stream yields no output. Only
    /// <see cref="EncryptionType.AesCbc256_HmacSha256_B64"/> is accepted.
    /// </summary>
    public static async Task DecryptToAsync(AttachmentKey key, Stream source, Stream plaintextStream, CancellationToken cancellationToken = default)
    {
        var iv = new byte[IvByteLength]; // IV is public; kept as a plain array for CreateDecryptor.
        using var headerOwner = MemoryOwner<byte>.Allocate(HeaderByteLength);
        using var copyBuffer = MemoryOwner<byte>.Allocate(CopyBufferByteLength);

        try
        {
            await source.ReadExactlyAsync(headerOwner.Memory, cancellationToken);

            if (headerOwner.Span[0] != (byte)EncryptionType.AesCbc256_HmacSha256_B64)
            {
                throw new CryptographicException(
                    $"Unsupported attachment encryption type: {headerOwner.Span[0]}.");
            }

            headerOwner.Span.Slice(TypeByteLength, IvByteLength).CopyTo(iv);

            using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key.MacKey);
            hmac.AppendData(iv);

            var tmpFile = Path.GetTempFileName();
            await using var tmpFileStream = new FileStream(tmpFile,
                new FileStreamOptions
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.ReadWrite,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose,
                });

            int read;
            while ((read = await source.ReadAsync(copyBuffer.Memory, cancellationToken)) > 0)
            {
                hmac.AppendData(copyBuffer.Span[..read]);
                await tmpFileStream.WriteAsync(copyBuffer.Memory[..read], cancellationToken);
            }

            Span<byte> computedMac = stackalloc byte[MacByteLength];
            hmac.GetHashAndReset(computedMac);
            if (!CryptographicOperations.FixedTimeEquals(
                    computedMac,
                    headerOwner.Span.Slice(TypeByteLength + IvByteLength, MacByteLength)))
            {
                throw new CryptographicException("Attachment MAC validation failed.");
            }

            tmpFileStream.Position = 0;
            using var aes = Aes.Create();
            aes.SetKey(key.AesKey);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            await using var cryptoStream = new CryptoStream(tmpFileStream, decryptor, CryptoStreamMode.Read, leaveOpen: true);
            await cryptoStream.CopyToAsync(plaintextStream, cancellationToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(copyBuffer.Span);
        }
    }
}
