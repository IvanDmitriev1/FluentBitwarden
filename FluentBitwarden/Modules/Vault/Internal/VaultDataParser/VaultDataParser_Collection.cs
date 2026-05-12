using BitwardenApi.Cryptography;
using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Vault.Internal.VaultDataParser;

public static partial class VaultDataParser
{
    public static VaultCollection ParseAndDecryptCollection(ref readonly VaultCollectionDto dto, DecryptedUserKey decryptedUserKey)
    {
        ArgumentNullException.ThrowIfNull(dto.EncryptedName);

        return new VaultCollection
        {
            Id = dto.Id,
            Name = CryptographyService.DecryptString(dto.EncryptedName, decryptedUserKey.Key),
            HidePasswords = dto.HidePasswords,
            ReadOnly = dto.ReadOnly,
            Manage = dto.Manage
        };
    }
}
