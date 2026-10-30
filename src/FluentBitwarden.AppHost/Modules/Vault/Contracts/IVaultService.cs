using BitwardenApi.Client;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Operations;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultService
{
    IUnlockedVault Open(BitwardenAccountContext accountContext, IAccountKeySession keySession);

    Task<VaultSyncResult> SyncAsync();
    Task SyncCipherAsync(CipherId id, CancellationToken cancellationToken);
    Task SyncFolderAsync(FolderId id, CancellationToken cancellationToken);
    Task DeleteCipherAsync(CipherId id, DateTimeOffset revisionDate, CancellationToken cancellationToken);
    Task DeleteFolderAsync(FolderId id, DateTimeOffset revisionDate, CancellationToken cancellationToken);

    Task<Cipher> SaveCipherAsync(
        Cipher cipher,
        CancellationToken cancellationToken);
}
