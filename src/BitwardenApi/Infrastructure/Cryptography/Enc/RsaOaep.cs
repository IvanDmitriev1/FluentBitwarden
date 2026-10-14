using System.Security.Cryptography;

namespace BitwardenApi.Infrastructure.Cryptography.Enc;

internal static class RsaOaep
{
    public static byte[] Decrypt(
        in EncStringParts parts,
        RSA privateKey)
    {
        RSAEncryptionPadding padding = ValidateAndGetPadding(in parts);
        return privateKey.Decrypt(parts.Data, padding);
    }

    public static int DecryptTo(
        in EncStringParts parts,
        RSA privateKey,
        Span<byte> destination)
    {
        RSAEncryptionPadding padding = ValidateAndGetPadding(in parts);

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

    private static RSAEncryptionPadding ValidateAndGetPadding(in EncStringParts parts)
    {
        RSAEncryptionPadding padding = parts.Type switch
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

        return padding;
    }
}
