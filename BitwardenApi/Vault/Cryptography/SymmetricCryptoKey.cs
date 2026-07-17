using BitwardenApi.Vault.Cryptography;
using System.Security.Cryptography;

namespace BitwardenApi.Vault.Cryptography;

/// <summary>
/// A decrypted symmetric vault key: the account user key or an organization key.
/// Encrypts vault items directly, or wraps per-cipher individual keys.
/// </summary>
public abstract class SymmetricCryptoKey(byte[] key) : IDisposable
{
    private bool _disposed;

    public ReadOnlySpan<byte> Key
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return key;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;

        if (disposing)
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }
}

public static class SymmetricCryptoKeyExtensions
{
    extension(in EncString value)
    {
        public string Decode(SymmetricCryptoKey key) => value.Decode(key.Key);
    }
}
