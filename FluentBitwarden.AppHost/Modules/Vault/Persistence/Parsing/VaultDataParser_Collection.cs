using BitwardenApi.Cryptography;

namespace FluentBitwarden.Modules.Vault.Internal.VaultDataParser;

public static partial class VaultDataParser
{
    public static VaultCollection ParseAndDecryptCollection(
        ref readonly VaultCollectionDto dto,
        scoped ReadOnlySpan<byte> key)
    {
        return new VaultCollection
        {
            Id = dto.Id,
            Name = dto.EncryptedName.Decode(key),
            HidePasswords = dto.HidePasswords,
            ReadOnly = dto.ReadOnly,
            Manage = dto.Manage
        };
    }
}
