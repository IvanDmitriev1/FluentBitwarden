using BitwardenApi.Vault.Cryptography;
using BitwardenApi.Vault.Items.Contracts;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

partial class VaultDataParser
{
    public static VaultFolder ParseAndDecryptFolder(ref readonly VaultFolderResponse dto, UnlockedUserKey decryptedUserKey)
    {
        return new VaultFolder
        {
            Id = dto.Id,
            Name = dto.EncryptedName.Decode(decryptedUserKey),
            RevisionDate = dto.RevisionDate
        };
    }
}
