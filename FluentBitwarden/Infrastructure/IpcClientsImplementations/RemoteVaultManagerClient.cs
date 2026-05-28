using BitwardenApi.Models;
using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Vault.Abstractions;
using System.Collections;

namespace FluentBitwarden.Infrastructure.IpcClientsImplementations;

[Fody.ConfigureAwait(false)]
internal sealed class RemoteVaultManagerClient(IIpcClient client) : IVaultManagerClient
{
    public ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default)
    {
        return client.SendAsync<VaultSyncResult>(IpcMessageTypes.Vault.Sync, cancellationToken);
    }

    public ValueTask<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default)
    {
        return client.SendAsync<VaultCipherQuery, VaultCipher[]>(query, cancellationToken);
    }

    public async ValueTask<VaultCipher?> GetCipherAsync(CipherId cipherId, CancellationToken cancellationToken = default)
    {
        var result = await client.SendAsync<GetVaultCipherRequest, IpcOptional<VaultCipher>>(new GetVaultCipherRequest(cipherId),
            cancellationToken);

        return result.Value;
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