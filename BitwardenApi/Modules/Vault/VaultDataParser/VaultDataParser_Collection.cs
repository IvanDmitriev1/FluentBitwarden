using BitwardenApi.Cryptography;
using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.VaultDataParser;

public static partial class VaultDataParser
{
    public static VaultCollection ParseAndDecryptCollection(in CollectionDto dto, DecryptedUserKey decryptedUserKey)
    {
        ArgumentNullException.ThrowIfNull(dto.EncryptedName);

        return new VaultCollection
        {
            Id = dto.Id,
            Name = DecryptField(dto.EncryptedName, decryptedUserKey.Key),
            HidePasswords = dto.HidePasswords,
            ReadOnly = dto.ReadOnly,
            Manage = dto.Manage
        };
    }
}
