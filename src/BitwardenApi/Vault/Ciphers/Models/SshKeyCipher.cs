using MemoryPack;




using BitwardenApi.Primitives;
namespace BitwardenApi.Vault.Ciphers.Models;
[MemoryPackable]
public sealed partial class SshKeyCipher : Cipher
{
    public override CipherType Type => CipherType.SshKey;

    public OpenSshPublicKey PublicKey { get; set; } = OpenSshPublicKey.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string KeyFingerprint { get; set; } = string.Empty;
}
