using System.Security.Cryptography;
using System.Text;

namespace BitwardenApi.Cryptography.Kdf;

internal static class Pbkdf2Kdf
{
    public static void Derive(ReadOnlySpan<char> password, ReadOnlySpan<char> salt, int iterations, Span<byte> destination)
    {
        int saltByteCount = System.Text.Encoding.UTF8.GetByteCount(salt);
        Span<byte> saltBytes = stackalloc byte[saltByteCount];

        _ = System.Text.Encoding.UTF8.GetBytes(salt, saltBytes);
        Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, destination, iterations, HashAlgorithmName.SHA256);
    }

    public static void Derive(ReadOnlySpan<byte> password, ReadOnlySpan<char> salt, int iterations, Span<byte> destination)
    {
        int saltByteCount = System.Text.Encoding.UTF8.GetByteCount(salt);
        Span<byte> saltBytes = stackalloc byte[saltByteCount];

        _ = System.Text.Encoding.UTF8.GetBytes(salt, saltBytes);
        Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, destination, iterations, HashAlgorithmName.SHA256);
    }
}
