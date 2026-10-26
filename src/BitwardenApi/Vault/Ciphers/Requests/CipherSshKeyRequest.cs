using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Vault.Ciphers.Requests;
public sealed class CipherSshKeyRequest
{
    public required EncString PrivateKey { get; init; }
    public required EncString PublicKey { get; init; }
    public required EncString KeyFingerprint { get; init; }
}
