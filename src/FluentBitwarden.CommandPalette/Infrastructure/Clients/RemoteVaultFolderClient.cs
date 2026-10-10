using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class RemoteVaultFolderClient(IIpcClient ipcClient) : IVaultFolderClient
{
    public Task<VaultFolder[]> GetFoldersAsync(
        GetVaultFoldersRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetVaultFoldersRequest, VaultFolder[]>(request, cancellationToken);
}
