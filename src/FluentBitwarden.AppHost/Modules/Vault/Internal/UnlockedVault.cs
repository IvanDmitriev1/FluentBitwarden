using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Internal;

internal sealed class UnlockedVault(UserId userId, VaultData data) : IUnlockedVault, IDisposable
{
    private readonly VaultData _data = data;

    public UserId UserId { get; } = userId;

    public void Dispose()
    {
        _data.Ciphers.Clear();
        _data.Folders.Clear();
        _data.Collections.Clear();
    }

    public VaultCipher? GetCipher(CipherId id)
    {
        return _data.Ciphers.FirstOrDefault(c => c.Id == id);
    }

    public VaultCipher[] GetCiphers(VaultCipherQuery query) =>
        _data.FilterCiphers(query);

    public VaultFolder[] GetFolders()
    {
        return _data.Folders.ToArray();
    }
}
