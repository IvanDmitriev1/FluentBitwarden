using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Modules.Vault.Models;

namespace FluentBitwarden.Modules.Vault.Abstractions;

public interface IVaultReaderRepository
{
    public delegate void CipherVisitor<in TState>(
        TState state,
        ref readonly CipherDto dto,
        ReadOnlySpan<byte> payload);

    void ReadAllCiphers<TState>(UserId userId, TState stateObj, CipherVisitor<TState> onCipher);

    IEnumerable<FolderDto> GetAllFolders(UserId userId);
    IEnumerable<CollectionDto> GetAllCollections(UserId userId);
}