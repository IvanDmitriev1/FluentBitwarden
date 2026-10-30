using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.CommandPalette.Infrastructure.Clients;

internal sealed class RemoteVaultFolderClient(IIpcClient ipcClient) : IVaultFolderClient
{
    public Task<Folder[]> GetFoldersAsync(
        GetVaultFoldersRequest request,
        CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<GetVaultFoldersRequest, Folder[]>(request, cancellationToken);
}
