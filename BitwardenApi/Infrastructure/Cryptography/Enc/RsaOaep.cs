using System.Security.Cryptography;

namespace BitwardenApi.Infrastructure.Cryptography.Enc;

internal static class RsaOaep
{
    public static int DecryptTo(
        in EncStringParts parts,
        RSA privateKey,
        Span<byte> destination)
    {
        var padding = parts.Type switch
        {
            EncryptionType.Rsa2048_OaepSha256_B64 => RSAEncryptionPadding.OaepSHA256,
            EncryptionType.Rsa2048_OaepSha1_B64 => RSAEncryptionPadding.OaepSHA1,
            EncryptionType.Rsa2048_OaepSha256_HmacSha256_B64 or
                EncryptionType.Rsa2048_OaepSha1_HmacSha256_B64 =>
                throw new CryptographicException($"Unsupported signed RSA EncString type: {parts.Type}."),
            _ => throw new CryptographicException($"Unsupported RSA EncString type: {parts.Type}.")
        };

        if (!parts.Iv.IsEmpty)
            throw new CryptographicException("RSA EncString IV was not expected.");

        if (!parts.Mac.IsEmpty)
            throw new CryptographicException("RSA EncString MAC was not expected.");

        if (!privateKey.TryDecrypt(parts.Data, destination, padding, out int bytesWritten))
        {
            if (destination.Length < privateKey.KeySize / 8)
            {
                throw new ArgumentException(
                    "Destination span was too small for the decrypted plaintext.", nameof(destination));
            }

            throw new CryptographicException("EncString RSA-OAEP decryption failed.");
        }

        destination[bytesWritten..].Clear();
        return bytesWritten;
    }
}
