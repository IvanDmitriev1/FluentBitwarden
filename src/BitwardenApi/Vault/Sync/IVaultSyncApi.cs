using BitwardenApi.Client;
using BitwardenApi.Vault.Sync.Responses;

namespace BitwardenApi.Vault.Sync;

public interface IVaultSyncApi
{
    Task<DateTimeOffset> GetRevisionDateAsync(BitwardenAccountContext accountContext, CancellationToken cancellationToken = default);

    Task<VaultSyncResponse> GetSyncAsync(BitwardenAccountContext accountContext, CancellationToken cancellationToken = default);
}
