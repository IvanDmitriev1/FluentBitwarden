using BitwardenApi.Infrastructure.Cryptography;

namespace FluentBitwarden.Modules.Vault.Internal.VaultDataParser;

public static partial class VaultDataParser
{
    public static VaultFolder ParseAndDecryptFolder(ref readonly VaultFolderDto dto, DecryptedUserKey decryptedUserKey)
    {
        return new VaultFolder
        {
            Id = dto.Id,
            Name = dto.EncryptedName.Decode(decryptedUserKey.Key),
            RevisionDate = dto.RevisionDate
        };
    }
}
