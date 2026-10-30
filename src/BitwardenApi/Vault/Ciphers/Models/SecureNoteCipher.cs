using MemoryPack;




namespace BitwardenApi.Vault.Ciphers.Models;
/// <remarks>Notes carries the secure note text via <see cref="Cipher.Notes"/>.</remarks>
[MemoryPackable]
public sealed partial class SecureNoteCipher : Cipher
{
    public override CipherType Type => CipherType.SecureNote;
}
