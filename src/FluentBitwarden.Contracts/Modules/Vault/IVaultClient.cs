using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.Contracts.Modules.Vault;

public interface IVaultClient
{
    Task<VaultSyncResult> SyncAsync(
        SyncVaultRequest request,
        CancellationToken cancellationToken = default);

    Task<VaultFolder[]> GetFoldersAsync(
        GetVaultFoldersRequest request,
        CancellationToken cancellationToken = default);

    Task<VaultCipher[]> SearchCiphersAsync(
        VaultCipherQuery query,
        CancellationToken cancellationToken = default);

    Task<VaultCipher?> GetCipherAsync(
        GetVaultCipherRequest request,
        CancellationToken cancellationToken = default);

    Task<VaultCipher?> SaveCipherAsync(
        SaveVaultCipherRequest request,
        CancellationToken cancellationToken = default);

    Task DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default);
}
