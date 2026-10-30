using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;

namespace FluentBitwarden.AppHost.Modules.Vault.Internal;

internal sealed class UnlockedVault(VaultData data) : IUnlockedVault
{
    private readonly Lock _lock = new();
    private VaultData _data = data;

    public void Dispose()
    {
        lock (_lock)
        {
            Clear(_data);
        }
    }

    public Cipher? GetCipher(CipherId id)
    {
        lock (_lock)
        {
            return _data.Ciphers.FirstOrDefault(c => c.Id == id);
        }
    }

    public Cipher[] GetCiphers(VaultCipherQuery query)
    {
        lock (_lock)
        {
            return _data.FilterCiphers(query);
        }
    }

    public Folder? GetFolder(FolderId id)
    {
        lock (_lock)
        {
            return _data.Folders.FirstOrDefault(f => f.Id == id);
        }
    }

    public Folder[] GetFolders()
    {
        lock (_lock)
        {
            return _data.Folders.ToArray();
        }
    }

    public void ReplaceContents(List<Cipher> ciphers, List<Folder> folders, List<Collection> collections)
    {
        lock (_lock)
        {
            var retired = _data;
            _data = new VaultData(ciphers, folders, collections);
            Clear(retired);
        }
    }

    public void UpsertCipher(Cipher cipher)
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

    public void UpsertFolder(Folder folder)
    {
        lock (_lock)
        {
            int index = _data.Folders.FindIndex(f => f.Id == folder.Id);
            if (index < 0)
                _data.Folders.Add(folder);
            else
                _data.Folders[index] = folder;
        }
    }

    public void RemoveCipher(CipherId id)
    {
        lock (_lock)
            _data.Ciphers.RemoveAll(cipher => cipher.Id == id);
    }

    public void RemoveFolder(FolderId id)
    {
        lock (_lock)
        {
            _data.Folders.RemoveAll(folder => folder.Id == id);
            foreach (Cipher cipher in _data.Ciphers.Where(cipher => cipher.FolderId == id))
                cipher.FolderId = FolderId.Empty;
        }
    }

    private static void Clear(VaultData data)
    {
        data.Ciphers.Clear();
        data.Folders.Clear();
        data.Collections.Clear();
    }
}
