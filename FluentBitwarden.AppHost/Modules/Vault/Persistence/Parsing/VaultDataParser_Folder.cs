using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

public static partial class VaultDataParser
{
    public static VaultFolder ParseAndDecryptFolder(ref readonly VaultFolderDto dto, DecryptedUserKey decryptedUserKey)
    {
        return new VaultFolder
        {
            Id = dto.Id,
            Name = dto.EncryptedName.Decode(decryptedUserKey),
            RevisionDate = dto.RevisionDate
        };
    }
}
