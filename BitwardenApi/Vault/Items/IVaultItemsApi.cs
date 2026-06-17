namespace BitwardenApi.Vault.Items;

public interface IVaultItemsApi
{
    Task<DateTimeOffset> GetRevisionDateAsync(CancellationToken cancellationToken = default);
    Task<VaultSyncResponse> GetSyncAsync(CancellationToken cancellationToken = default);
    Task<VaultCipherDto> GetCipherAsync(CipherId cipherId, CancellationToken cancellationToken = default);
    Task DeleteCipherAsync(CipherId cipherId, CancellationToken cancellationToken = default);
}
