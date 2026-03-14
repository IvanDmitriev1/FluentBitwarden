using System.Security.Cryptography;
using System.Text;
using BitwaredApi.Abstractions;
using BitwaredApi.Crypto.Enc;
using BitwaredApi.Crypto.Kdf;
using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;
using BitwaredApi.Utils;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwaredApi.Services;

internal sealed class CryptoService : ICryptoService
{
    private const int MaxStackPlaintextByteCount = 256;

    public MasterPasswordAuth DeriveMasterPasswordAuth(string email, string masterPassword, KdfConfigModel kdfConfig, string? kdfSalt = null)
    {
        string normalizedEmail = NormalizeEmail(email);
        string normalizedSalt = string.IsNullOrWhiteSpace(kdfSalt)
            ? normalizedEmail
            : NormalizeEmail(kdfSalt);
        byte[] masterKey = kdfConfig.Type switch
        {
            KdfType.Pbkdf2Sha256 => Pbkdf2Kdf.Derive(masterPassword, normalizedSalt, kdfConfig.Iterations, 32),
            KdfType.Argon2id => Argon2idKdf.Derive(
                masterPassword,
                normalizedSalt,
                kdfConfig.Iterations,
                kdfConfig.Memory ?? 64,
                kdfConfig.Parallelism ?? 4,
                32),
            _ => throw new CryptographicException($"Unsupported KDF type: {kdfConfig.Type}."),
        };

        byte[] stretchedMasterKey = StretchMasterKey(masterKey);
        using var authHashOwner = MemoryOwner<byte>.Allocate(32);
        Span<byte> authHash = authHashOwner.Span[..32];

        Pbkdf2Kdf.Derive(masterKey, masterPassword, 1, authHash);

        return new MasterPasswordAuth(
            masterKey,
            stretchedMasterKey,
            Convert.ToBase64String(authHash));
    }

    public byte[] DecryptUserKey(EncString encryptedUserKey, ReadOnlySpan<byte> stretchedMasterKey)
    {
        EncStringParts parsed = encryptedUserKey.Parse();
        return AesCbcHmac.Decrypt(parsed, stretchedMasterKey);
    }

    public byte[] DecryptRsaWrappedKey(EncString encryptedUserKey, ReadOnlySpan<byte> privateKeyPkcs8)
    {
        ArgumentNullException.ThrowIfNull(encryptedUserKey);

        EncStringParts parsed = encryptedUserKey.Parse();

        if (parsed.Type is not EncStringType.Rsa2048_OaepSha1_B64
            and not EncStringType.Rsa2048_OaepSha256_B64
            and not EncStringType.Rsa2048_OaepSha1_HmacSha256_B64
            and not EncStringType.Rsa2048_OaepSha256_HmacSha256_B64)
        {
            throw new CryptographicException($"Unsupported RSA EncString type: {parsed.Type}.");
        }

        int cipherByteLength = CryptoEncoding.GetBase64DecodedLength(parsed.Data, "RSA EncString ciphertext");
        Span<byte> cipherBytes = stackalloc byte[cipherByteLength];

        _ = CryptoEncoding.DecodeBase64(parsed.Data, cipherBytes, "RSA EncString ciphertext");
        using RSA rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);

        RSAEncryptionPadding padding = parsed.Type is EncStringType.Rsa2048_OaepSha256_B64 or EncStringType.Rsa2048_OaepSha256_HmacSha256_B64
            ? RSAEncryptionPadding.OaepSHA256
            : RSAEncryptionPadding.OaepSHA1;

        return rsa.Decrypt(cipherBytes, padding);
    }

    public byte[] UnwrapKey(EncString encryptedKey, ReadOnlySpan<byte> wrappingKey)
    {
        EncStringParts parsed = encryptedKey.Parse();
        return AesCbcHmac.Decrypt(parsed, wrappingKey);
    }

    public string DecryptString(EncString encryptedValue, ReadOnlySpan<byte> key)
    {
        EncStringParts parsed = encryptedValue.Parse();
        int maxPlaintextLength = CryptoEncoding.GetBase64DecodedLength(parsed.Data, "EncString ciphertext");
        using MemoryOwner<byte> pooledPlaintext = maxPlaintextLength <= MaxStackPlaintextByteCount
            ? MemoryOwner<byte>.Empty
            : MemoryOwner<byte>.Allocate(maxPlaintextLength);

        Span<byte> plaintext = maxPlaintextLength <= MaxStackPlaintextByteCount
            ? stackalloc byte[maxPlaintextLength]
            : pooledPlaintext.Span[..maxPlaintextLength];

        int bytesWritten = AesCbcHmac.DecryptTo(parsed, key, plaintext);

        try
        {
            return Encoding.UTF8.GetString(plaintext[..bytesWritten]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public string CreateFingerprintPhrase(string email, ReadOnlySpan<byte> publicKey)
    {
        string normalizedEmail = NormalizeEmail(email);
        Span<byte> byteBuffers = stackalloc byte[42];
        Span<byte> publicKeyHash = byteBuffers[..32];
        Span<byte> material = byteBuffers.Slice(32, 10);
        Span<char> phrase = stackalloc char[24];

        SHA256.HashData(publicKey, publicKeyHash);
        Hkdf.Expand(publicKeyHash, normalizedEmail, material);

        int destinationIndex = 0;

            for (int i = 0; i < material.Length; i++)
            {
                byte value = material[i];
                phrase[destinationIndex++] = CryptoEncoding.ToHexLower(value >> 4);
                phrase[destinationIndex++] = CryptoEncoding.ToHexLower(value & 0x0F);

            if ((i & 1) == 1 && i < material.Length - 1)
            {
                phrase[destinationIndex++] = '-';
            }
        }

        return new string(phrase);
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static byte[] StretchMasterKey(ReadOnlySpan<byte> masterKey)
    {
        byte[] stretched = new byte[64];
        Hkdf.Expand(masterKey, "enc", stretched.AsSpan(0, 32));
        Hkdf.Expand(masterKey, "mac", stretched.AsSpan(32, 32));
        return stretched;
    }
}
