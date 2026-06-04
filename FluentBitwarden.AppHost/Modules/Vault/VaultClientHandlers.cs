using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Models;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault;

internal class VaultClientHandlers(
    IVaultWorkspace vaultWorkspace,
    IUnlockedVaultReader unlockedVaultReader,
    IVaultSynchronizer vaultSynchronizer,
    IUnlockedAccountAccessor unlockedAccountAccessor) : IVaultClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.Vault.Sync)]
    public async ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default)
    {
        var result = await vaultSynchronizer.SyncAsync(unlockedAccountAccessor.UserKey, cancellationToken: cancellationToken);
        if (result == VaultSyncResult.Synced)
        {
            vaultWorkspace.Reload(unlockedAccountAccessor.UserKey);
        }

        return result;
    }

    public ValueTask<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default)
    {
        var ciphers = unlockedVaultReader.GetCiphers(query);
        return ValueTask.FromResult(ciphers);
    }

    public ValueTask<VaultCipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default)
    {
        var cipher = unlockedVaultReader.GetCipher(request.CipherId);
        return ValueTask.FromResult(cipher);
    }

    [IpcMessageHandler(IpcMessageTypes.Vault.GetFolders)]
    public ValueTask<VaultFolder[]> GetFoldersAsync(CancellationToken cancellationToken = default)
    {
        var folders = unlockedVaultReader.GetFolders();
        return ValueTask.FromResult(folders);
    }

    [IpcMessageHandler(IpcMessageTypes.Vault.GetCollections)]
    public ValueTask<VaultCollection[]> GetCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var collections = unlockedVaultReader.GetCollections();
        return ValueTask.FromResult(collections);
    }
}