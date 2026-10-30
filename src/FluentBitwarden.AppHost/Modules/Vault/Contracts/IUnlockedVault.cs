using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IUnlockedVault : IDisposable
{
    Cipher? GetCipher(CipherId id);
    Cipher[] GetCiphers(VaultCipherQuery query);

    Folder? GetFolder(FolderId id);
    Folder[] GetFolders();

    void ReplaceContents(List<Cipher> ciphers, List<Folder> folders, List<Collection> collections);

    void UpsertCipher(Cipher cipher);
    void UpsertFolder(Folder folder);
    void RemoveCipher(CipherId id);
    void RemoveFolder(FolderId id);
}
