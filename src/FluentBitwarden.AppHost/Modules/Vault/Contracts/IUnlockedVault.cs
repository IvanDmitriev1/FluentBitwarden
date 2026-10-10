using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IUnlockedVault : IDisposable
{
    BitwardenAccountContext AccountContext { get; }


    VaultCipher? GetCipher(CipherId id);

    VaultCipher[] GetCiphers(VaultCipherQuery query);

    VaultFolder[] GetFolders();

    void ReplaceContents(List<VaultCipher> ciphers, List<VaultFolder> folders, List<VaultCollection> collections);

    void UpsertCipher(VaultCipher cipher);
}
