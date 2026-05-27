using BitwardenApi.Models;
using FluentBitwarden.Contracts.Vault.Models;

namespace FluentBitwarden.Contracts.Vault.Abstractions;

public interface IVaultManagerClient
{
    ValueTask<VaultSyncResult> SyncVaultAsync(CancellationToken cancellationToken = default);

    ValueTask<List<VaultCipher>> SearchCiphersAsync(VaultCipherQuery query, CancellationToken cancellationToken = default);
    ValueTask<VaultCipher?> GetCipherAsync(CipherId cipherId, CancellationToken cancellationToken = default);
    ValueTask<List<VaultFolder>> GetFoldersAsync(CancellationToken cancellationToken = default);
    ValueTask<List<VaultCollection>> GetCollectionsAsync(CancellationToken cancellationToken = default);
}
