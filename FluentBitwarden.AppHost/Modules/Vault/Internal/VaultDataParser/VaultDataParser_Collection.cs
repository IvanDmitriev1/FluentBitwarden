using BitwardenApi.Cryptography;
using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Vault.Internal.VaultDataParser;

public static partial class VaultDataParser
{
    public static VaultCollection ParseAndDecryptCollection(ref readonly VaultCollectionDto dto, DecryptedUserKey decryptedUserKey)
    {
        return new VaultCollection
        {
            Id = dto.Id,
            Name = dto.EncryptedName.Decode(decryptedUserKey.Key),
            HidePasswords = dto.HidePasswords,
            ReadOnly = dto.ReadOnly,
            Manage = dto.Manage
        };
    }
}
