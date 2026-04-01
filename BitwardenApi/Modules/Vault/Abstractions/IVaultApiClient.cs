using BitwardenApi.Modules.Vault.Models;
using BitwardenApi.Shared.Context;
using BitwardenApi.Shared.Transport;

namespace BitwardenApi.Modules.Vault.Abstractions;

public interface IVaultApiClient
{
    Task<ApiStreamResponse> GetSyncAsync(
        BitwardenEnvironment environment,
        CancellationToken cancellationToken = default);

    Task<ApiStreamResponse> GetCipherAsync(
        BitwardenEnvironment environment,
        CipherId cipherId,
        CancellationToken cancellationToken = default);

    Task<ApiStreamResponse> GetAllCiphersAsync(
        BitwardenEnvironment environment,
        CancellationToken cancellationToken = default);

    Task DeleteCipherAsync(
        BitwardenEnvironment environment,
        CipherId cipherId,
        CancellationToken cancellationToken = default);
}
