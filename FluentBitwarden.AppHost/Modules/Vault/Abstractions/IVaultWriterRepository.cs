using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

internal interface IVaultWriterRepository
{
    void WriteFolders(ReadOnlySpan<VaultFolderDto> folders);
    void WriteCollections(ReadOnlySpan<VaultCollectionDto> collections);
    void WriteCiphers(ReadOnlySpan<VaultCipherDto> ciphers);
    void DeleteVaultData();
}