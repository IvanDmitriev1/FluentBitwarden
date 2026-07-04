using FluentBitwarden.Contracts.Modules.Vault.Synchronization;
using FluentBitwarden.Contracts.Modules.Vault.Workspace;

namespace FluentBitwarden.Contracts.Modules.Vault;

public interface IVaultClient
{
    ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default);

    ValueTask<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default);
    ValueTask<VaultCipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default);
    ValueTask DownloadCipherAttachmentAsync(
        DownloadVaultCipherAttachmentRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<VaultFolder[]> GetFoldersAsync(CancellationToken cancellationToken = default);
    ValueTask<VaultCollection[]> GetCollectionsAsync(CancellationToken cancellationToken = default);
}