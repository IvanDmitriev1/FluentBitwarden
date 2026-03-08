using System.Security.Cryptography;

namespace BitwaredApi.Crypto.Kdf;

internal static class Pbkdf2Kdf
{
    public static byte[] Derive(string password, string salt, int iterations, int outputBytes)
        => Rfc2898DeriveBytes.Pbkdf2(
            password,
            System.Text.Encoding.UTF8.GetBytes(salt),
            iterations,
            HashAlgorithmName.SHA256,
            outputBytes);

    public static byte[] Derive(ReadOnlySpan<byte> password, string salt, int iterations, int outputBytes)
        => Rfc2898DeriveBytes.Pbkdf2(
            password,
            System.Text.Encoding.UTF8.GetBytes(salt),
            iterations,
            HashAlgorithmName.SHA256,
            outputBytes);

    public static byte[] Derive(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, int outputBytes)
        => Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            outputBytes);
}
