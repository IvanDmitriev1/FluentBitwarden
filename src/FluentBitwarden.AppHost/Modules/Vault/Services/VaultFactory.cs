using BitwardenApi.Client;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.AppHost.Modules.Vault.Internal;
using FluentBitwarden.AppHost.Modules.Vault.Persistence;

namespace FluentBitwarden.AppHost.Modules.Vault.Services;

internal sealed class VaultFactory(VaultReaderRepository vaultReaderRepository) : IVaultFactory
{
    public IUnlockedVault Open(BitwardenAccountContext accountContext, IAccountKeySession keySession)
    {
        if (accountContext.UserId != keySession.UserId)
            throw new InvalidOperationException("The selected account and account key must have the same user ID.");

        keySession.UpdateOrganizations(vaultReaderRepository.LoadOrganizationKeys(accountContext.UserId));
        return new UnlockedVault(VaultDataLoader.LoadCached(vaultReaderRepository, keySession.UserId, keySession));
    }
}
