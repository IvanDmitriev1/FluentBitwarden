
using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Cryptography.Keys;

public static class CipherKeyExtensions
{
    public static string Decode(this in EncString value, CipherKey key) => value.Decode(key.Key);

    public static int DecodeEncStringInPlace(this Span<byte> encodedUtf8, CipherKey key)
        => encodedUtf8.DecodeEncStringInPlace(key.Key);
}
