using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.Abstractions;

public interface ISyncDataWriter
{
    void WriteFolder(ref readonly FolderDto dto);
    void WriteCollection(ref readonly CollectionDto dto);
    void WriteCipher(ref readonly CipherDto dto, ReadOnlySpan<byte> payload);
}
