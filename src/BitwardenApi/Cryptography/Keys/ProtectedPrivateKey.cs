using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Cryptography.Keys;

public readonly record struct ProtectedPrivateKey(EncString Value)
{
    public static ProtectedPrivateKey Create(EncString value) => new(value);
}
