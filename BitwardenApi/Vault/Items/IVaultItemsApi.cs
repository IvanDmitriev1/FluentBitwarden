namespace BitwardenApi.Vault.Items;

public interface IVaultItemsApi
{
    Task<DateTimeOffset> GetRevisionDateAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default);

    Task<VaultSyncResponse> GetSyncAsync(
        BitwardenAccountContext accountContext,
        CancellationToken cancellationToken = default);

    Task<VaultCipherDto> GetCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        CancellationToken cancellationToken = default);

    Task DeleteCipherAsync(
        BitwardenAccountContext accountContext,
        CipherId cipherId,
        CancellationToken cancellationToken = default);
}
