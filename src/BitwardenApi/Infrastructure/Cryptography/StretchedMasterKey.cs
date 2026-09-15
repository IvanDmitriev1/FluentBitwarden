using System.Security.Cryptography;
using BitwardenApi.Infrastructure.Cryptography.Kdf;

namespace BitwardenApi.Infrastructure.Cryptography;

/// <summary>
/// The 512-bit stretched master key: the <see cref="MasterKey"/> expanded via HKDF into a
/// 256-bit AES key (first half) and a 256-bit HMAC key (second half). Used to decrypt the
/// protected user key. Owns exact-size key material; the creator must dispose it (zeroes the buffer).
/// </summary>
public readonly ref struct StretchedMasterKey
{
    private const int KeyLength = 64;

    private readonly byte[] _key;

    private StretchedMasterKey(byte[] key) => _key = key;

    internal static StretchedMasterKey FromMasterKey(ReadOnlySpan<byte> masterKey)
    {
        var key = new byte[KeyLength];
        try
        {
            Hkdf.Expand(masterKey, "enc", key.AsSpan(..32));
            Hkdf.Expand(masterKey, "mac", key.AsSpan(32, 32));
            return new StretchedMasterKey(key);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(key);
            throw;
        }
    }

    /// <summary>The full 512-bit key: first 256 bits AES key, last 256 bits HMAC key.</summary>
    public ReadOnlySpan<byte> Span => _key.AsSpan();

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_key.AsSpan());
    }
}