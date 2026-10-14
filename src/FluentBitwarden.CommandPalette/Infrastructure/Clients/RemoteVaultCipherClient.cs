using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Attachments;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class RemoteVaultCipherClient(IIpcClient ipcClient) : IVaultCipherClient
{
    public Task<VaultCipher[]> SearchCiphersAsync(
        VaultCipherQuery query,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<VaultCipherQuery, VaultCipher[]>(query, cancellationToken);

    public Task<VaultCipher?> GetCipherAsync(
        GetVaultCipherRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetVaultCipherRequest, VaultCipher?>(request, cancellationToken);

    public Task<VaultCipher> SaveCipherAsync(
        SaveVaultCipherRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<SaveVaultCipherRequest, VaultCipher>(request, cancellationToken);

    public async Task DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await ipcClient.SendAsync<DownloadVaultCipherAttachmentRequest, IpcVoid>(request, cancellationToken);
    }
}
