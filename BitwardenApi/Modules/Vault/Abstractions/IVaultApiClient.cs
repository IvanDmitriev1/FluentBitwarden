using BitwardenApi.Modules.Vault.Models;
using BitwardenApi.Modules.Vault.SyncParser;
using BitwardenApi.Shared.Transport;

namespace BitwardenApi.Modules.Vault.Abstractions;

public interface IVaultApiClient
{
     Task<DateTimeOffset> GetRevisionDateAsync(
         BitwardenEnvironment environment,
         CancellationToken cancellationToken = default);

    Task<SyncPayload> GetSyncAsync(
        BitwardenEnvironment environment,
        CancellationToken cancellationToken = default);

    Task<ApiStreamResponse> GetCipherAsync(
        BitwardenEnvironment environment,
        CipherId cipherId,
        CancellationToken cancellationToken = default);

    Task DeleteCipherAsync(
        BitwardenEnvironment environment,
        CipherId cipherId,
        CancellationToken cancellationToken = default);
}
