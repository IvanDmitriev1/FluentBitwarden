using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class RemoteVaultClient(IIpcClient ipcClient) : IVaultClient
{
    public ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<VaultSyncResult>(IpcMessageTypes.Vault.Sync, cancellationToken);

    public ValueTask<VaultCipher[]> SearchCiphersAsync(
        VaultCipherQuery query,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<VaultCipherQuery, VaultCipher[]>(query, cancellationToken);

    public ValueTask<VaultCipher?> GetCipherAsync(
        GetVaultCipherRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetVaultCipherRequest, VaultCipher?>(request, cancellationToken);

    public ValueTask<VaultCipher?> SaveCipherAsync(
        SaveVaultCipherRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<SaveVaultCipherRequest, VaultCipher?>(request, cancellationToken);

    public async ValueTask DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await ipcClient.SendAsync<DownloadVaultCipherAttachmentRequest, IpcVoid>(request, cancellationToken);
    }
}
