using BitwardenApi.Vault.Cryptography;
using System.Security.Cryptography;

namespace BitwardenApi.Vault.Cryptography;

/// <summary>
/// A decrypted symmetric vault key: the account user key or an organization key.
/// Encrypts vault items directly, or wraps per-cipher individual keys.
/// </summary>
public abstract class DecryptedVaultKey(byte[] key) : IDisposable
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
        if (_disposed)
            return;

        _disposed = true;
        CryptographicOperations.ZeroMemory(key);
    }
}

public static class DecryptedVaultKeyExtensions
{
    extension(in EncString value)
    {
        public string Decode(DecryptedVaultKey key) => value.Decode(key.Key);
    }
}
