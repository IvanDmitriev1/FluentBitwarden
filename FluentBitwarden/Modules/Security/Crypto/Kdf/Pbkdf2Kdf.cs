using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace FluentBitwarden.Modules.Security.Crypto.Kdf;

internal static class Pbkdf2Kdf
{
    private const int MaxStackSaltByteCount = 256;

    public static byte[] Derive(ReadOnlySpan<char> password, ReadOnlySpan<char> salt, int iterations, int outputBytes)
    {
        byte[] output = new byte[outputBytes];
        Derive(password, salt, iterations, output);
        return output;
    }

    public static void Derive(ReadOnlySpan<char> password, ReadOnlySpan<char> salt, int iterations, Span<byte> destination)
    {
        int saltByteCount = GetValidatedSaltByteCount(salt);
        Span<byte> saltBytes = stackalloc byte[saltByteCount];

        Encoding.UTF8.GetBytes(salt, saltBytes);
        Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, destination, iterations, HashAlgorithmName.SHA256);
    }

    public static void Derive(ReadOnlySpan<byte> password, ReadOnlySpan<char> salt, int iterations, Span<byte> destination)
    {
        int saltByteCount = GetValidatedSaltByteCount(salt);
        Span<byte> saltBytes = stackalloc byte[saltByteCount];

        Encoding.UTF8.GetBytes(salt, saltBytes);
        Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, destination, iterations, HashAlgorithmName.SHA256);
    }

    private static int GetValidatedSaltByteCount(ReadOnlySpan<char> salt)
    {
        int saltByteCount = Encoding.UTF8.GetByteCount(salt);
        Debug.Assert(saltByteCount < MaxStackSaltByteCount, $"Salt byte count {saltByteCount} exceeds the maximum allowed for stack allocation.");

        return saltByteCount;
    }
}