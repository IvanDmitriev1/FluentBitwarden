using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultManager
{
    IUnlockedVault Open(BitwardenAccountContext accountContext, IAccountKeySession keySession);

    Task<VaultSyncResult> SyncAsync(IUnlockedVault vault, CancellationToken ct);
    Task<VaultCipher> SaveCipherAsync(
        IUnlockedVault vault,
        IAccountKeySession keySession,
        VaultCipher cipher,
        CancellationToken ct);
}
