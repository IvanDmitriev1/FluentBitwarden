using BitwardenApi.Primitives;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.Views.Vault;

[Fody.ConfigureAwait(false)]
internal sealed class RemoteVaultClient(IIpcClient client) : IVaultClient
{
    public ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default)
    {
        return client.SendAsync<VaultSyncResult>(IpcMessageTypes.Vault.Sync, cancellationToken);
    }

    public ValueTask<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default)
    {
        return client.SendAsync<VaultCipherQuery, VaultCipher[]>(query, cancellationToken);
    }

    public ValueTask<VaultCipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default)
    {
        return client.SendAsync<GetVaultCipherRequest, VaultCipher?>(request, cancellationToken);
    }

    public ValueTask<VaultFolder[]> GetFoldersAsync(CancellationToken cancellationToken = default)
    {
        return client.SendAsync<VaultFolder[]>(IpcMessageTypes.Vault.GetFolders, cancellationToken);
    }

    public ValueTask<VaultCollection[]> GetCollectionsAsync(CancellationToken cancellationToken = default)
    {
        return client.SendAsync<VaultCollection[]>(IpcMessageTypes.Vault.GetCollections, cancellationToken);
    }
}