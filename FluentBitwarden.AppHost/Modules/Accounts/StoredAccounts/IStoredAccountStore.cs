using BitwardenApi.Models;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts.Models;
using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts;

internal interface IStoredAccountStore
{
    AccountProfile[] GetAccounts();
    AccountProfile? GetAccount(UserId userId);
    AccountKeyMaterial? GetKeyMaterial(UserId userId);

    void Save(AccountProfile profile, AccountKeyMaterial keyMaterial);
    void Remove(UserId userId);
}
