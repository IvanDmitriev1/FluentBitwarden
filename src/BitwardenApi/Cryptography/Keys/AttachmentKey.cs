using System.Security.Cryptography;

using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Cryptography.Keys;

/// <summary>
/// Decrypted per-attachment key from AttachmentDownloadResponse.ProtectedAttachmentKey.
/// Heap-allocated because attachment download/decryption is asynchronous; zeroed on Dispose.
/// </summary>
public sealed class AttachmentKey : IDisposable
{
    private const int MacByteLength = 32;
    private const int EncryptionKeyByteLength = 32;
    private const int KeyByteLength = 64;

    private readonly byte[] _key;
    private bool _disposed;

    private AttachmentKey(byte[] key) => _key = key;

    internal ReadOnlySpan<byte> Key
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _key.AsSpan();
        }
    }

    /// <summary>The 32-byte AES key half. Heap-backed, so it is safe to use across await points.</summary>
    internal ReadOnlySpan<byte> AesKey
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _key.AsSpan(..EncryptionKeyByteLength);
        }
    }

    /// <summary>The 32-byte HMAC key half. Heap-backed, so it is safe to use across await points.</summary>
    internal ReadOnlySpan<byte> MacKey
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _key.AsSpan(EncryptionKeyByteLength, MacByteLength);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CryptographicOperations.ZeroMemory(_key.AsSpan());
    }

    public static AttachmentKey Create(
        in EncString encryptedKey,
        CipherKey cipherKey)
    {
        var key = new byte[KeyByteLength];
        encryptedKey.DecodeTo(cipherKey.Key, key);
        return new AttachmentKey(key);
    }
}