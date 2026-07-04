using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Modules.Vault.Attachments;
using FluentBitwarden.AppHost.Modules.Vault.Workspace.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.AppHost.Infrastructure.Ipc.Handlers;

internal sealed class VaultIpcHandler(
    IVaultSessionCoordinator vaultSessionCoordinator,
    IUnlockedVaultReader unlockedVaultReader,
    IVaultCipherAttachmentDownloadService attachmentDownloadService) : IVaultClient, IIpcRequestsHandler
{
    private bool HasUnlockedSession => vaultSessionCoordinator.TryGetUnlockedSession(out _);

    [IpcMessageHandler(IpcMessageTypes.Vault.Sync)]
    public ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default) =>
        vaultSessionCoordinator.SyncVaultAsync(cancellationToken);

    public ValueTask<VaultCipher[]> SearchCiphersAsync(
        VaultCipherQuery query,
        CancellationToken cancellationToken = default)
    {
        var ciphers = HasUnlockedSession ? unlockedVaultReader.GetCiphers(query) : [];
        return ValueTask.FromResult(ciphers);
    }

    public ValueTask<VaultCipher?> GetCipherAsync(
        GetVaultCipherRequest request,
        CancellationToken cancellationToken = default)
    {
        var cipher = HasUnlockedSession ? unlockedVaultReader.GetCipher(request.CipherId) : null;
        return ValueTask.FromResult(cipher);
    }

    public async ValueTask DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default) =>
        await attachmentDownloadService.DownloadAsync(request, cancellationToken);

    [IpcMessageHandler(IpcMessageTypes.Vault.GetFolders)]
    public ValueTask<VaultFolder[]> GetFoldersAsync(CancellationToken cancellationToken = default)
    {
        var folders = HasUnlockedSession ? unlockedVaultReader.GetFolders() : [];
        return ValueTask.FromResult(folders);
    }

    [IpcMessageHandler(IpcMessageTypes.Vault.GetCollections)]
    public ValueTask<VaultCollection[]> GetCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var collections = HasUnlockedSession ? unlockedVaultReader.GetCollections() : [];
        return ValueTask.FromResult(collections);
    }
}
