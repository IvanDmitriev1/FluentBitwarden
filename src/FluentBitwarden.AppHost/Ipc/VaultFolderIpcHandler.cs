using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Folders;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class VaultFolderIpcHandler(IAppSessionLease appSessionLease) : IVaultFolderClient, IIpcRequestsHandler
{
    public Task<Folder[]> GetFoldersAsync(GetVaultFoldersRequest request, CancellationToken cancellationToken = default)
    {
        var unlocked = appSessionLease.RequireUnlockedState();

        return Task.FromResult(unlocked.Vault.GetFolders());
    }
}
