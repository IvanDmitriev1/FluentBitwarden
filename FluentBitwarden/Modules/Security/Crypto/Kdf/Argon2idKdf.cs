using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace FluentBitwarden.Modules.Security.Crypto.Kdf;

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
        int passwordByteCount = Encoding.UTF8.GetByteCount(password);
        int saltByteCount = Encoding.UTF8.GetByteCount(salt);

        byte[] passwordBytes = new byte[passwordByteCount];
        byte[] saltBytes = new byte[saltByteCount];
        byte[] output = new byte[destination.Length];

        _ = Encoding.UTF8.GetBytes(password, passwordBytes);
        _ = Encoding.UTF8.GetBytes(salt, saltBytes);

        Argon2BytesGenerator generator = new();
        Argon2Parameters parameters = new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
            .WithSalt(saltBytes)
            .WithIterations(iterations)
            .WithParallelism(parallelism)
            .WithMemoryAsKB(checked(memoryMiB * 1024))
            .Build();

        try
        {
            generator.Init(parameters);
            generator.GenerateBytes(passwordBytes, output, 0, destination.Length);
            output.CopyTo(destination);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(saltBytes);
            CryptographicOperations.ZeroMemory(output);
        }
    }
}
