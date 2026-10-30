
namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

partial class VaultDataParser
{
    public static Folder ParseAndDecryptFolder(ref readonly FolderResponse dto, UnlockedUserKey decryptedUserKey)
    {
        return new Folder
        {
            Id = dto.Id,
            Name = dto.EncryptedName.Decode(decryptedUserKey),
            RevisionDate = dto.RevisionDate
        };
    }
}
