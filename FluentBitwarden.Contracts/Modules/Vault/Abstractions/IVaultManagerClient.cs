using FluentBitwarden.Contracts.Modules.Vault.Models;

namespace FluentBitwarden.Contracts.Modules.Vault.Abstractions;

public interface IVaultManagerClient
{
    ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default);

    ValueTask<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default);
    ValueTask<VaultCipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default);
    ValueTask<VaultFolder[]> GetFoldersAsync(CancellationToken cancellationToken = default);
    ValueTask<VaultCollection[]> GetCollectionsAsync(CancellationToken cancellationToken = default);
}
