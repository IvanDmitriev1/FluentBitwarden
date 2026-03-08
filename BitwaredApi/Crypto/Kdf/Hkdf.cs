using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace BitwaredApi.Crypto.Kdf;

internal static class Hkdf
{
    public static byte[] Expand(ReadOnlySpan<byte> inputKeyMaterial, string info, int outputLength)
    {
        return Expand(inputKeyMaterial, Encoding.UTF8.GetBytes(info), outputLength);
    }

    public static byte[] Expand(ReadOnlySpan<byte> inputKeyMaterial, ReadOnlySpan<byte> info, int outputLength)
    {
        byte[] output = new byte[outputLength];
        HkdfBytesGenerator generator = new(new Sha256Digest());
        generator.Init(new HkdfParameters(inputKeyMaterial.ToArray(), null, info.ToArray()));
        generator.GenerateBytes(output, 0, output.Length);
        return output;
    }
}
