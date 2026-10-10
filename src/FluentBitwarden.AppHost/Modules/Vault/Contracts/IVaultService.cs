using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Operations;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultService
{
    IUnlockedVault Open(BitwardenAccountContext accountContext, IAccountKeySession keySession);

    Task<VaultSyncResult> SyncAsync(
        IUnlockedVault vault,
        IAccountKeySession keySession,
        CancellationToken cancellationToken);

    Task<VaultCipher> SaveCipherAsync(
        IUnlockedVault vault,
        IAccountKeySession keySession,
        VaultCipher cipher,
        CancellationToken cancellationToken);
}
