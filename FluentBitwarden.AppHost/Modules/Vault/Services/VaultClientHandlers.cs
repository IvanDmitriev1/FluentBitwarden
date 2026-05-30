using BitwardenApi.Models;
using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Vault.Abstractions;
using FluentBitwarden.Contracts.Vault.Models;
using FluentBitwarden.Modules.Vault.Abstractions;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal class VaultClientHandlers(IVaultService vaultService) : IVaultManagerClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.Vault.Sync)]
    public ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default) =>
        vaultService.SyncVaultAsync(cancellationToken);

    public ValueTask<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default)
    {
        var ciphers = vaultService.GetCiphers(query);
        return ValueTask.FromResult(ciphers);

    }

    public ValueTask<VaultCipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default)
    {
        var cipher = vaultService.GetCipher(request.CipherId);
        return ValueTask.FromResult(cipher);
    }

    [IpcMessageHandler(IpcMessageTypes.Vault.GetFolders)]
    public ValueTask<VaultFolder[]> GetFoldersAsync(CancellationToken cancellationToken = default)
    {
        var folders = vaultService.GetFolders();
        return ValueTask.FromResult(folders);
    }

    [IpcMessageHandler(IpcMessageTypes.Vault.GetCollections)]
    public ValueTask<VaultCollection[]> GetCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var collections = vaultService.GetCollections();
        return ValueTask.FromResult(collections);
    }
}