using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Contracts.Modules.Vault.Models;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace;

internal sealed class VaultWorkspace(VaultLoader vaultLoader) : IVaultWorkspace, IUnlockedVaultReader
{
    private LoadedVaultData _vaultData = new([], [], []);

    public UserId OpenedForUserId { get; private set; } = UserId.Empty;
    public bool IsOpen => OpenedForUserId != UserId.Empty;

    public void Open(DecryptedUserKey userKey)
    {
        var data = vaultLoader.Load(userKey);
        Volatile.Write(ref _vaultData, data);

        OpenedForUserId = userKey.UserId;
    }

    public void Reload(DecryptedUserKey userKey)
    {
        var data = vaultLoader.Load(userKey);
        Volatile.Write(ref _vaultData, data);

        OpenedForUserId = userKey.UserId;
    }

    public void Close()
    {
        Volatile.Write(ref _vaultData, new LoadedVaultData([], [], []));
        OpenedForUserId = UserId.Empty;
    }

    public VaultCipher? GetCipher(CipherId id)
    {
        var data = Volatile.Read(ref _vaultData);
        return data.CiphersById.GetValueOrDefault(id);
    }

    public VaultCipher[] GetCiphers(VaultCipherQuery query)
    {
        var data = Volatile.Read(ref _vaultData);

        IEnumerable<VaultCipher> result = data.CiphersById.Values;

        if (query.FavoritesOnly)
            result = result.Where(static x => x.Favorite);

        if (!query.IncludeDeleted)
            result = result.Where(static x => x.DeletedDate is null);

        if (query.FolderId != FolderId.Empty)
            result = result.Where(x => x.FolderId == query.FolderId);

        if (query.CipherType is not null)
            result = result.Where(x => x.Type == query.CipherType.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchText))
            result = result.Where(x => x.MatchesSearchText(query.SearchText));

        result = result.ApplySort(query.SortField, query.SortDirection);

        if (query.Limit is not null)
            result = result.Take(query.Limit.Value);

        return result.ToArray();
    }

    public VaultFolder[] GetFolders()
    {
        var data = Volatile.Read(ref _vaultData);
        return data.Folders
            .OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public VaultCollection[] GetCollections()
    {
        var snapshot = Volatile.Read(ref _vaultData);

        return snapshot.Collections
            .OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}