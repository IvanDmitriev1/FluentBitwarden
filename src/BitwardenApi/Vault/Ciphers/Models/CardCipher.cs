using MemoryPack;




namespace BitwardenApi.Vault.Ciphers.Models;
[MemoryPackable]
public sealed partial class CardCipher : Cipher
{
    public override CipherType Type => CipherType.Card;

    public string? CardholderName { get; set; }
    public string? Brand { get; set; }
    public string? Number { get; set; }
    public string? ExpMonth { get; set; }
    public string? ExpYear { get; set; }
    public string? Code { get; set; }
}
