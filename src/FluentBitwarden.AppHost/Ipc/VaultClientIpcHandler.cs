using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.AppSession.Services;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class VaultClientIpcHandler(
    IAppSessionService appSessionService,
    IVaultManager vaultManager) : IVaultClient, IIpcRequestsHandler
{
    public async Task<VaultSyncResult> SyncAsync(SyncVaultRequest request, CancellationToken cancellationToken = default)
    {
        using var lease = appSessionService.TryAcquireUnlockedSessionLease();
        if (lease is null)
            return VaultSyncResult.Failed;

        return await vaultManager.SyncAsync(lease.Vault, cancellationToken);
    }

    public Task<VaultFolder[]> GetFoldersAsync(GetVaultFoldersRequest request, CancellationToken cancellationToken = default)
    {
        using var lease = appSessionService.TryAcquireUnlockedSessionLease();
        if (lease is null)
            return Task.FromResult(Array.Empty<VaultFolder>());

        return Task.FromResult(lease.Vault.GetFolders());
    }

    public Task<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default)
    {
        using var lease = appSessionService.TryAcquireUnlockedSessionLease();
        if (lease is null)
            return Task.FromResult(Array.Empty<VaultCipher>());

        return Task.FromResult(lease.Vault.GetCiphers(query));
    }

    public Task<VaultCipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default)
    {
        using var lease = appSessionService.TryAcquireUnlockedSessionLease();
        if (lease is null)
            return Task.FromResult<VaultCipher?>(null);

        return Task.FromResult(lease.Vault.GetCipher(request.CipherId));
    }

    public async Task<VaultCipher> SaveCipherAsync(SaveVaultCipherRequest request, CancellationToken cancellationToken = default)
    {


        throw new NotImplementedException();
    }

    public Task DownloadCipherAttachmentAsync(DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
