using System.Text;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace BitwaredApi.Crypto.Kdf;

internal static class Argon2idKdf
{
    public static byte[] Derive(string password, string salt, int iterations, int memoryMiB, int parallelism, int outputBytes)
    {
        byte[] output = new byte[outputBytes];
        Argon2BytesGenerator generator = new();
        Argon2Parameters parameters = new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
            .WithSalt(Encoding.UTF8.GetBytes(salt))
            .WithIterations(iterations)
            .WithParallelism(parallelism)
            .WithMemoryAsKB(checked(memoryMiB * 1024))
            .Build();

        generator.Init(parameters);
        generator.GenerateBytes(Encoding.UTF8.GetBytes(password), output, 0, output.Length);
        return output;

    }
}
