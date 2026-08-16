using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;

namespace BitwardenApi.Vault.Cryptography;

/// <summary>
/// The effective decryption key for a single vault cipher: the base vault key when the cipher
/// has no individual key, otherwise the decrypted VaultCipherResponse.ProtectedCipherKey.
/// Owns exact-size key material; the creator must dispose it (zeroes the buffer).
/// </summary>
public readonly ref struct CipherKey
{
    private readonly byte[] _key;
    private readonly int _length;

    private CipherKey(byte[] key, int length)
    {
        _key = key;
        _length = length;
    }

    internal ReadOnlySpan<byte> Key => _key.AsSpan(.._length);

    public static CipherKey Create(in EncString encryptedKey, SymmetricCryptoKey baseKey)
    {
        if (encryptedKey.IsEmpty)
        {
            var baseKeyBytes = baseKey.Key;
            var key = new byte[baseKeyBytes.Length];
            try
            {
                baseKeyBytes.CopyTo(key);
                return new CipherKey(key, baseKeyBytes.Length);
            }
            catch
            {
                CryptographicOperations.ZeroMemory(key);
                throw;
            }
        }

        var decryptedKey = new byte[encryptedKey.MaxPlaintextByteCount];
        try
        {
            int bytesWritten = encryptedKey.DecodeTo(baseKey.Key, decryptedKey);
            return new CipherKey(decryptedKey, bytesWritten);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(decryptedKey);
            throw;
        }
    }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_key.AsSpan());
    }
}

public static class CipherKeyExtensions
{
    extension(in EncString value)
    {
        public string Decode(CipherKey key) => value.Decode(key.Key);
    }

    public static int DecodeEncStringInPlace(this Span<byte> encodedUtf8, CipherKey key)
        => encodedUtf8.DecodeEncStringInPlace(key.Key);
}