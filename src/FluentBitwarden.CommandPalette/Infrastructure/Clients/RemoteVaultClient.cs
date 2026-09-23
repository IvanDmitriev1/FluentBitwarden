using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class RemoteVaultClient(IIpcClient ipcClient) : IVaultClient
{
    public Task<VaultSyncResult> SyncVaultAsync(
        SyncVaultRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<SyncVaultRequest, VaultSyncResult>(request, cancellationToken);

    public Task<VaultFolder[]> GetFoldersAsync(
        GetVaultFoldersRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetVaultFoldersRequest, VaultFolder[]>(request, cancellationToken);

    public Task<VaultCipher[]> SearchCiphersAsync(
        VaultCipherQuery query,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<VaultCipherQuery, VaultCipher[]>(query, cancellationToken);

    public Task<VaultCipher?> GetCipherAsync(
        GetVaultCipherRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetVaultCipherRequest, VaultCipher?>(request, cancellationToken);

    public Task<VaultCipher?> SaveCipherAsync(
        SaveVaultCipherRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<SaveVaultCipherRequest, VaultCipher?>(request, cancellationToken);

    public async Task DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await ipcClient.SendAsync<DownloadVaultCipherAttachmentRequest, IpcVoid>(request, cancellationToken);
    }
}
