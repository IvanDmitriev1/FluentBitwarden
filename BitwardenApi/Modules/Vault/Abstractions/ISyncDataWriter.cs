using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.Abstractions;

public interface ISyncDataWriter
{
    void WriteFolder(in FolderDto dto);
    void WriteCollection(in CollectionDto dto);
    void WriteCipher(in CipherDto row);
}