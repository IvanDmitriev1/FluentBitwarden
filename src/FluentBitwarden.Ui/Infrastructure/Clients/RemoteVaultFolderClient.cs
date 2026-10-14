using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.Infrastructure.Clients;

internal sealed class RemoteVaultFolderClient(IIpcClient client) : IVaultFolderClient
{
    public Task<VaultFolder[]> GetFoldersAsync(
        GetVaultFoldersRequest request,
        CancellationToken cancellationToken = default) =>
        client.SendAsync<GetVaultFoldersRequest, VaultFolder[]>(request, cancellationToken);
}
