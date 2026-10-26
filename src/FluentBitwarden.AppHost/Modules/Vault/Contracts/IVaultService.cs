using BitwardenApi.Client;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Operations;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultService
{
    IUnlockedVault Open(BitwardenAccountContext accountContext, IAccountKeySession keySession);

    Task<VaultSyncResult> SyncAsync(
        IUnlockedSessionLease lease,
        CancellationToken cancellationToken);

    Task<Cipher> SaveCipherAsync(
        IUnlockedSessionLease lease,
        Cipher cipher,
        CancellationToken cancellationToken);
}
