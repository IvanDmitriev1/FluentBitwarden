using BitwardenApi.Internal;

namespace BitwardenApi.Vault;

public interface IVaultApiClient
{
    Task<ApiStreamResponse> GetSyncAsync(
        GetSyncRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiStreamResponse> GetCipherAsync(
        GetCipherRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiStreamResponse> GetAllCiphersAsync(
        GetAllCiphersRequest request,
        CancellationToken cancellationToken = default);

    Task CreateCipherAsync(
        CreateCipherRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateCipherAsync(
        UpdateCipherRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCipherAsync(
        DeleteCipherRequest request,
        CancellationToken cancellationToken = default);
}
