
using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Cryptography.Keys;

public static class SymmetricCryptoKeyExtensions
{
    extension(in EncString value)
    {
        public string Decode(SymmetricCryptoKey key) => value.Decode(key.Key);
    }
}
