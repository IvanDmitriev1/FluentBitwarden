using MemoryPack;




using BitwardenApi.Vault.Ciphers.Serialization;
namespace BitwardenApi.Vault.Ciphers.Models;
[MemoryPackable]
public sealed partial class LoginCipher : Cipher
{
    public override CipherType Type => CipherType.Login;

    public string? Username { get; set; }
    public string? Password { get; set; }

    [TotpValueFormatter]
    public TotpValue? Totp { get; set; }
    public List<LoginUri> Uris { get; set; } = [];
    public Fido2Credential? Fido2Credential { get; set; }
}
