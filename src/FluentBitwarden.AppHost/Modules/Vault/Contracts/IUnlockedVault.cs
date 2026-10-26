using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IUnlockedVault : IDisposable
{
    Cipher? GetCipher(CipherId id);

    Cipher[] GetCiphers(VaultCipherQuery query);

    Folder[] GetFolders();

    void ReplaceContents(List<Cipher> ciphers, List<Folder> folders, List<Collection> collections);

    void UpsertCipher(Cipher cipher);
}
