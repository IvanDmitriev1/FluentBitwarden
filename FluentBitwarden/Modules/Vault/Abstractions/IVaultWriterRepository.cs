using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

internal interface IVaultWriterRepository
{
    void WriteFolder(ref readonly VaultFolderDto dto);
    void WriteCollection(ref readonly VaultCollectionDto dto);
    void WriteCipher(ref readonly VaultCipherDto dto, ReadOnlySpan<byte> payload);
    void DeleteVaultData();
}