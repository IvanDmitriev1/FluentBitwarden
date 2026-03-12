using BitwaredApi.Models.Vault;

namespace FluentBitwarden.Abstractions.Storage;

internal interface IVaultSnapshotWriteStore
{
    ValueTask SaveSyncAsync(
        EncryptedSyncSnapshot snapshot,
        CancellationToken cancellationToken = default);

    ValueTask ClearAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
