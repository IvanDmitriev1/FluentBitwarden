using BitwardenApi.Vault.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

public static partial class VaultDataParser
{
    public static VaultFolder ParseAndDecryptFolder(ref readonly VaultFolderResponse dto, UserKey decryptedUserKey)
    {
        return new VaultFolder
        {
            Id = dto.Id,
            Name = dto.EncryptedName.Decode(decryptedUserKey),
            RevisionDate = dto.RevisionDate
        };
    }
}
