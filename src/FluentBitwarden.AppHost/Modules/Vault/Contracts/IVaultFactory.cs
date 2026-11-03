using BitwardenApi.Client;
using FluentBitwarden.AppHost.Modules.Account.Contracts;

namespace FluentBitwarden.AppHost.Modules.Vault.Contracts;

public interface IVaultFactory
{
    IUnlockedVault Open(BitwardenAccountContext accountContext, IAccountKeySession keySession);
}
