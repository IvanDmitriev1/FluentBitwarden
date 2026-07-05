using System.Security.Cryptography;

namespace BitwardenApi.Infrastructure.Cryptography;

/// <summary>
/// An account's RSA private key, decrypted from the protected private key. Decrypts asymmetric
/// values such as an organization key that was wrapped with the matching public key.
/// Owns the underlying <see cref="RSA"/>; the creator must dispose it.
/// </summary>
public sealed class PrivateKey : IDisposable
{
    private readonly RSA _rsa;
    private bool _disposed;

    private PrivateKey(RSA rsa) => _rsa = rsa;

    /// <summary>Imports a PKCS#8 or (fallback) PKCS#1 RSA private key.</summary>
    public static PrivateKey Import(ReadOnlySpan<byte> privateKeyBytes)
    {
        try
        {
            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
            return new PrivateKey(rsa);
        }
        catch (CryptographicException pkcs8Exception)
        {
            try
            {
                var rsa = RSA.Create();
                rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
                return new PrivateKey(rsa);
            }
            catch (CryptographicException pkcs1Exception)
            {
                throw new CryptographicException(
                    "The decrypted Bitwarden private key could not be imported as PKCS#8 or PKCS#1 RSA key.",
                    pkcs1Exception.InnerException ?? pkcs8Exception);
            }
        }
    }

    /// <summary>Decrypts an RSA-wrapped value into <paramref name="destination"/>; returns bytes written.</summary>
    public int Decrypt(in AsymmetricEncString value, Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return value.DecodeRsaTo(_rsa, destination);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _rsa.Dispose();
    }
}
