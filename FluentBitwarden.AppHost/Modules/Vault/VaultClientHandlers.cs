using FluentBitwarden.AppHost.Modules.Vault.Synchronization;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Models;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault;

internal class VaultClientHandlers(
    IUnlockedVaultReader unlockedVaultReader,
    IVaultSynchronizer vaultSynchronizer) : IVaultClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.Vault.Sync)]
    public ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default) =>
        vaultSynchronizer.SyncAsync(cancellationToken);

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