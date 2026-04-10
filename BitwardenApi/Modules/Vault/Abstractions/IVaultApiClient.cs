using BitwardenApi.Modules.Vault.Models;
using BitwardenApi.Modules.Vault.SyncParser;
using BitwardenApi.Shared.Transport;

namespace BitwardenApi.Modules.Vault.Abstractions;

public interface IVaultApiClient
{
    Task<DateTimeOffset> GetRevisionDateAsync(
        CancellationToken cancellationToken = default);

    Task<SyncPayload> GetSyncAsync(
        CancellationToken cancellationToken = default);

    Task<ApiStreamResponse> GetCipherAsync(
        CipherId cipherId,
        CancellationToken cancellationToken = default);

    Task DeleteCipherAsync(
        CipherId cipherId,
        CancellationToken cancellationToken = default);
}
