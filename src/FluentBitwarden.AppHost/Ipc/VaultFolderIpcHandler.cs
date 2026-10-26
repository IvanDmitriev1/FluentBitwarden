using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class VaultFolderIpcHandler(IAppSessionService appSessionService) : IVaultFolderClient, IIpcRequestsHandler
{
    public Task<Folder[]> GetFoldersAsync(GetVaultFoldersRequest request, CancellationToken cancellationToken = default)
    {
        using var lease = appSessionService.RequireUnlockedSessionLease();

        return Task.FromResult(lease.Vault.GetFolders());
    }
}
