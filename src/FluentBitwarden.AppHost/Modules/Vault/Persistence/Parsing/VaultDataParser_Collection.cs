
namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing;

partial class VaultDataParser
{
    public static Collection ParseAndDecryptCollection(
        ref readonly CollectionResponse dto,
        SymmetricCryptoKey key)
    {
        return new Collection
        {
            Id = dto.Id,
            Name = dto.EncryptedName.Decode(key),
            HidePasswords = dto.HidePasswords,
            ReadOnly = dto.ReadOnly,
            Manage = dto.Manage
        };
    }
}
