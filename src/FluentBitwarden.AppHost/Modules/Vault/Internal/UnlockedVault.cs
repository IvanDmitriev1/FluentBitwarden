using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Internal;

internal sealed class UnlockedVault(BitwardenAccountContext accountContext, VaultData data) : IUnlockedVault, IDisposable
{
    public BitwardenAccountContext AccountContext { get; } = accountContext;

    public void Dispose()
    {
        data.Ciphers.Clear();
        data.Folders.Clear();
        data.Collections.Clear();
    }

    public VaultCipher? GetCipher(CipherId id)
    {
        return data.Ciphers.FirstOrDefault(c => c.Id == id);
    }

    public VaultCipher[] GetCiphers(VaultCipherQuery query) =>
        data.FilterCiphers(query);

    public VaultFolder[] GetFolders()
    {
        return data.Folders.ToArray();
    }
}
