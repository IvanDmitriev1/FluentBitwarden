using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

public static partial class VaultDataParser
{
    public static VaultCollection ParseAndDecryptCollection(
        ref readonly VaultCollectionDto dto,
        DecryptedVaultKey key)
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
