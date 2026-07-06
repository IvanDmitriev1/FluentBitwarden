using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Models;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Modules.Vault.Workspace;

[Fody.ConfigureAwait(false)]
internal sealed class VaultWorkspace(
    VaultSynchronizer vaultSynchronizer,
    VaultLoader vaultLoader,
    VaultCipherSaver vaultCipherSaver) : IVaultWorkspace, IUnlockedVaultReader
{
    private WorkspaceState _state = WorkspaceState.Empty;

    public async ValueTask OpenAsync(
        BitwardenAccountContext accountContext,
        UserKey userKey,
        bool forceSync,
        CancellationToken cancellationToken)
    {
        Reload(userKey);

        var data = Volatile.Read(ref _state).Data;
        if (!forceSync && data.CiphersById.Count > 0)
            return;

        var result = await vaultSynchronizer.SyncAsync(
            accountContext,
            userKey,
            force: true,
            cancellationToken);

        if (result == VaultSyncResult.Synced)
        {
            Reload(userKey);
        }
    }

    public async Task<VaultSyncResult> SyncAsync(
        BitwardenAccountContext accountContext,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        var userKey = RequireUserKey();
        var result = await vaultSynchronizer.SyncAsync(accountContext, userKey, force, cancellationToken);

        if (result == VaultSyncResult.Synced)
            Reload(userKey);

        return result;
    }

    public async ValueTask<VaultCipher> SaveCipherAsync(
        BitwardenAccountContext accountContext,
        VaultCipher cipher,
        CancellationToken cancellationToken = default)
    {
        var userKey = RequireUserKey();
        var savedCipher = await vaultCipherSaver.SaveAsync(accountContext, userKey, cipher, cancellationToken);
        UpsertCipher(savedCipher);
        return savedCipher;
    }

    public void Close() => Volatile.Write(ref _state, WorkspaceState.Empty);

    public VaultCipher? GetCipher(CipherId id)
    {
        var data = Volatile.Read(ref _state).Data;
        return data.CiphersById.GetValueOrDefault(id);
    }

    public VaultCipher[] GetCiphers(VaultCipherQuery query)
    {
        var data = Volatile.Read(ref _state).Data;

        IEnumerable<VaultCipher> result = data.CiphersById.Values;

        if (query.FavoritesOnly)
            result = result.Where(static x => x.Favorite);

        if (!query.IncludeDeleted)
            result = result.Where(static x => x.DeletedDate is null);

        if (!query.FolderId.IsEmpty)
            result = result.Where(x => x.FolderId == query.FolderId);

        if (!query.CollectionId.IsEmpty)
        {
            if (!data.CipherIdsByCollectionId.TryGetValue(query.CollectionId, out var cipherIds))
                return [];

            result = result.Where(x => cipherIds.Contains(x.Id));
        }

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
        var data = Volatile.Read(ref _state).Data;
        return data.Folders
            .OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public VaultCollection[] GetCollections()
    {
        var snapshot = Volatile.Read(ref _state).Data;

        return snapshot.Collections
            .OrderBy(static x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private void Reload(UserKey userKey) =>
        Volatile.Write(ref _state, new WorkspaceState(userKey, vaultLoader.Load(userKey)));

    private void UpsertCipher(VaultCipher cipher)
    {
        var state = Volatile.Read(ref _state);
        var ciphersById = new Dictionary<CipherId, VaultCipher>(state.Data.CiphersById) { [cipher.Id] = cipher };
        Volatile.Write(ref _state, state with { Data = state.Data with { CiphersById = ciphersById } });
    }

    private UserKey RequireUserKey() =>
        Volatile.Read(ref _state).UserKey ?? throw new InvalidOperationException("Vault workspace is not open.");
}
