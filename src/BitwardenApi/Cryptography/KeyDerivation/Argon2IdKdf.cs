using Org.BouncyCastle.Crypto.Generators;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;

namespace BitwardenApi.Cryptography.KeyDerivation;

internal static class Argon2IdKdf
{
    public static void Derive(
        ReadOnlySpan<char> password,
        ReadOnlySpan<char> salt,
        int iterations,
        int memoryMiB,
        int parallelism,
        Span<byte> destination)
    {
        int memoryKiB = checked(memoryMiB * 1024);
        int passwordByteCount = System.Text.Encoding.UTF8.GetByteCount(password);
        int saltByteCount = System.Text.Encoding.UTF8.GetByteCount(salt);

        byte[] passwordBytes = new byte[passwordByteCount];
        byte[] saltBytes = new byte[saltByteCount];
        byte[] output = new byte[destination.Length];
        byte[] saltHash = [];

        _ = System.Text.Encoding.UTF8.GetBytes(password, passwordBytes);
        _ = System.Text.Encoding.UTF8.GetBytes(salt, saltBytes);

        try
        {
            saltHash = SHA256.HashData(saltBytes);
            Argon2BytesGenerator generator = new();
            Argon2Parameters parameters = new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
                .WithSalt(saltHash)
                .WithIterations(iterations)
                .WithParallelism(parallelism)
                .WithMemoryAsKB(memoryKiB)
                .Build();

            generator.Init(parameters);
            generator.GenerateBytes(passwordBytes, output, 0, destination.Length);
            output.CopyTo(destination);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(saltBytes);
            CryptographicOperations.ZeroMemory(saltHash);
            CryptographicOperations.ZeroMemory(output);
        }
    }
}
