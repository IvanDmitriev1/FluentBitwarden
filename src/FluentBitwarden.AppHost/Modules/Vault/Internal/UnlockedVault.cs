using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;

namespace FluentBitwarden.AppHost.Modules.Vault.Internal;

internal sealed class UnlockedVault(BitwardenAccountContext accountContext, VaultData data) : IUnlockedVault
{
    private readonly Lock _lock = new();
    private VaultData _data = data;

    public BitwardenAccountContext AccountContext { get; } = accountContext;

    public void Dispose()
    {
        lock (_lock)
        {
            Clear(_data);
        }
    }

    public VaultCipher? GetCipher(CipherId id)
    {
        lock (_lock)
        {
            return _data.Ciphers.FirstOrDefault(cipher => cipher.Id == id);
        }
    }

    public VaultCipher[] GetCiphers(VaultCipherQuery query)
    {
        lock (_lock)
        {
            return _data.FilterCiphers(query);
        }
    }

    public VaultFolder[] GetFolders()
    {
        lock (_lock)
        {
            return _data.Folders.ToArray();
        }
    }

    public void ReplaceContents(List<VaultCipher> ciphers, List<VaultFolder> folders, List<VaultCollection> collections)
    {
        lock (_lock)
        {
            var retired = _data;
            _data = new VaultData(ciphers, folders, collections);
            Clear(retired);
        }
    }

    public void UpsertCipher(VaultCipher cipher)
    {
        lock (_lock)
        {
            var index = _data.Ciphers.FindIndex(existing => existing.Id == cipher.Id);
            if (index < 0)
                _data.Ciphers.Add(cipher);
            else
                _data.Ciphers[index] = cipher;
        }
    }

    private static void Clear(VaultData data)
    {
        data.Ciphers.Clear();
        data.Folders.Clear();
        data.Collections.Clear();
    }
}
