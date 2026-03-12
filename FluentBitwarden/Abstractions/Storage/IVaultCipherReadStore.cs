using BitwaredApi.Models.Vault;

namespace FluentBitwarden.Abstractions.Storage;

internal interface IVaultCipherReadStore
{
    ValueTask<IReadOnlyList<EncryptedCipherRecord>> ListByAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    ValueTask<EncryptedCipherRecord?> GetByIdAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default);
}
