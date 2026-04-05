using BitwardenApi.Cryptography;
using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.VaultDataParser;

public static partial class VaultDataParser
{
    public static Folder ParseAndDecryptFolder(in FolderDto dto, DecryptedUserKey decryptedUserKey)
    {
        ArgumentNullException.ThrowIfNull(dto.EncryptedName);

        return new Folder
        {
            Id = dto.Id,
            Name = DecryptField(dto.EncryptedName, decryptedUserKey.Key),
            RevisionDate = dto.RevisionDate
        };
    }
}
