using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.Infrastructure.Clients;

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

    public async ValueTask DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await client.SendAsync<DownloadVaultCipherAttachmentRequest, IpcVoid>(
            request,
            cancellationToken);
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