namespace BitwardenApi.Vault.Items;

public interface IVaultItemsApi
{
    Task<DateTimeOffset> GetRevisionDateAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default);

    Task<VaultSyncResponse> GetSyncAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default);

    Task<VaultCipherResponse> GetCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        CancellationToken cancellationToken = default);

    Task<VaultCipherResponse> CreateCipherAsync(
        BitwardenAccountContext accountContext,
        VaultCipherRequest request,
        CancellationToken cancellationToken = default);

    Task<VaultCipherResponse> UpdateCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        VaultCipherRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        CancellationToken cancellationToken = default);
}
