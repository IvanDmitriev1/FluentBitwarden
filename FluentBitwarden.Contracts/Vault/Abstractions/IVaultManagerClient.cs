using BitwardenApi.Models;
using FluentBitwarden.Contracts.Vault.Models;

namespace FluentBitwarden.Contracts.Vault.Abstractions;

public interface IVaultManagerClient
{
    ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default);

    ValueTask<VaultCipher[]> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default);
    ValueTask<VaultCipher?> GetCipherAsync(GetVaultCipherRequest request, CancellationToken cancellationToken = default);
    ValueTask<VaultFolder[]> GetFoldersAsync(CancellationToken cancellationToken = default);
    ValueTask<VaultCollection[]> GetCollectionsAsync(CancellationToken cancellationToken = default);
}
