using BitwardenApi.Cryptography;
using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Internal.VaultDataParser;

public static partial class VaultDataParser
{
    public static Folder ParseAndDecryptFolder(ref readonly FolderDto dto, DecryptedUserKey decryptedUserKey)
    {
        ArgumentNullException.ThrowIfNull(dto.EncryptedName);

        return new Folder
        {
            Id = dto.Id,
            Name = CryptographyService.DecryptString(dto.EncryptedName, decryptedUserKey.Key),
            RevisionDate = dto.RevisionDate
        };
    }
}
