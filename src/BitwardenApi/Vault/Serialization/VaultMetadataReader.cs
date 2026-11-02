using BitwardenApi.Cryptography.Encryption;
using BitwardenApi.Cryptography.Keys;
using BitwardenApi.Vault.Collections.Models;
using BitwardenApi.Vault.Collections.Responses;
using BitwardenApi.Vault.Folders.Models;
using BitwardenApi.Vault.Folders.Responses;

namespace BitwardenApi.Vault.Serialization;

public static class VaultMetadataReader
{
    public static Folder ReadFolder(ref readonly FolderResponse dto, SymmetricCryptoKey key) => new()
    {
        Id = dto.Id,
        Name = dto.EncryptedName.Decode(key),
        RevisionDate = dto.RevisionDate
    };

    public static Collection ReadCollection(ref readonly CollectionResponse dto, SymmetricCryptoKey key) => new()
    {
        Id = dto.Id,
        Name = dto.EncryptedName.Decode(key),
        HidePasswords = dto.HidePasswords,
        ReadOnly = dto.ReadOnly,
        Manage = dto.Manage
    };
}
