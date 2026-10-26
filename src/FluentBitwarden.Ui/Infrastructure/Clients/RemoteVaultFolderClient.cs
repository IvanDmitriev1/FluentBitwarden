using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.Infrastructure.Clients;

internal sealed class RemoteVaultFolderClient(IIpcClient client) : IVaultFolderClient
{
    public Task<Folder[]> GetFoldersAsync(
        GetVaultFoldersRequest request,
        CancellationToken cancellationToken = default) =>
        client.SendAsync<GetVaultFoldersRequest, Folder[]>(request, cancellationToken);
}
