using System.Security.Cryptography;
using System.Text;
using BitwaredApi.Abstractions;
using BitwaredApi.Crypto.Kdf;
using BitwaredApi.Models.Auth;
using BitwaredApi.Models.Vault;

namespace BitwaredApi.Crypto.Enc;

public sealed class CryptoService : ICryptoService
{
    public MasterPasswordAuth DeriveMasterPasswordAuth(string email, string masterPassword, KdfConfigModel kdfConfig, string? kdfSalt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(masterPassword);

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
        byte[] authHash = Pbkdf2Kdf.Derive(masterKey, Encoding.UTF8.GetBytes(masterPassword), 1, 32);

        try
        {
            return new MasterPasswordAuth(
                masterKey,
                stretchedMasterKey,
                Convert.ToBase64String(authHash));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(authHash);
        }
    }

    public byte[] DecryptUserKey(EncString encryptedUserKey, byte[] stretchedMasterKey)
    {
        ArgumentNullException.ThrowIfNull(encryptedUserKey);
        ArgumentNullException.ThrowIfNull(stretchedMasterKey);

        ParsedEncString parsed = ParseRequired(encryptedUserKey.Value);
        return AesCbcHmac.Decrypt(parsed, stretchedMasterKey);
    }

    public byte[] DecryptRsaWrappedKey(EncString encryptedUserKey, byte[] privateKeyPkcs8)
    {
        ArgumentNullException.ThrowIfNull(encryptedUserKey);
        ArgumentNullException.ThrowIfNull(privateKeyPkcs8);

        ParsedEncString parsed = ParseRequired(encryptedUserKey.Value);

        if (parsed.Type is not EncStringType.Rsa2048_OaepSha1_B64
            and not EncStringType.Rsa2048_OaepSha256_B64
            and not EncStringType.Rsa2048_OaepSha1_HmacSha256_B64
            and not EncStringType.Rsa2048_OaepSha256_HmacSha256_B64)
        {
            throw new CryptographicException($"Unsupported RSA EncString type: {parsed.Type}.");
        }

        byte[] cipherBytes = Convert.FromBase64String(parsed.Data);

        try
        {
            using RSA rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);

            RSAEncryptionPadding padding = parsed.Type is EncStringType.Rsa2048_OaepSha256_B64 or EncStringType.Rsa2048_OaepSha256_HmacSha256_B64
                ? RSAEncryptionPadding.OaepSHA256
                : RSAEncryptionPadding.OaepSHA1;

            return rsa.Decrypt(cipherBytes, padding);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cipherBytes);
        }
    }

    public byte[]? UnwrapKey(EncString? encryptedKey, byte[] wrappingKey)
    {
        if (encryptedKey is null)
        {
            return null;
        }

        ParsedEncString parsed = ParseRequired(encryptedKey.Value);
        return AesCbcHmac.Decrypt(parsed, wrappingKey);
    }

    public string? DecryptString(EncString? encryptedValue, byte[] key)
    {
        if (encryptedValue is null)
        {
            return null;
        }

        if (!EncStringParser.IsSerialized(encryptedValue.Value))
        {
            return encryptedValue.Value;
        }

        byte[] plaintext = AesCbcHmac.Decrypt(ParseRequired(encryptedValue.Value), key);

        try
        {
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public string CreateFingerprintPhrase(string email, byte[] publicKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(publicKey);

        byte[] publicKeyHash = SHA256.HashData(publicKey);
        byte[] material = Hkdf.Expand(publicKeyHash, NormalizeEmail(email), 10);

        try
        {
            return string.Create(
                24,
                material,
                static (destination, source) =>
                {
                    string hex = Convert.ToHexString(source).ToLowerInvariant();
                    string phrase = string.Join('-', Enumerable.Range(0, 5).Select(i => hex.Substring(i * 4, 4)));
                    phrase.AsSpan().CopyTo(destination);
                });
        }
        finally
        {
            CryptographicOperations.ZeroMemory(publicKeyHash);
            CryptographicOperations.ZeroMemory(material);
        }
    }

    public void ZeroMemory(byte[]? buffer)
    {
        if (buffer is not null)
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static byte[] StretchMasterKey(ReadOnlySpan<byte> masterKey)
    {
        byte[] encKey = Hkdf.Expand(masterKey, "enc", 32);
        byte[] macKey = Hkdf.Expand(masterKey, "mac", 32);
        byte[] stretched = new byte[64];

        Buffer.BlockCopy(encKey, 0, stretched, 0, 32);
        Buffer.BlockCopy(macKey, 0, stretched, 32, 32);

        CryptographicOperations.ZeroMemory(encKey);
        CryptographicOperations.ZeroMemory(macKey);

        return stretched;
    }

    private static ParsedEncString ParseRequired(string value)
        => EncStringParser.TryParse(value, out ParsedEncString? parsed)
            ? parsed!
            : throw new CryptographicException("The provided value is not a valid Bitwarden EncString.");
}
