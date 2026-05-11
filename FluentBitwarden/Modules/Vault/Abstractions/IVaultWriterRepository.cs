using BitwardenApi.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

internal interface IVaultWriterRepository
{
    void WriteFolder(ref readonly FolderDto dto);
    void WriteCollection(ref readonly CollectionDto dto);
    void WriteCipher(ref readonly CipherDto dto, ReadOnlySpan<byte> payload);
    void DeleteVaultData();
}