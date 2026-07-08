using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts;

internal interface IAccountStore
{
    AccountProfile[] GetAccounts();
    AccountProfile? GetAccount(UserId userId);
    AccountProfileDetails? GetAccountProfileDetails(UserId userId);
    AccountKeyMaterial? GetKeyMaterial(UserId userId);
    RefreshToken GetRefreshToken(UserId userId);

    void Save(AccountProfile profile, AccountKeyMaterial keyMaterial);

    void SaveAuthenticatedAccount(
        AccountProfile profile,
        AccountKeyMaterial keyMaterial,
        RefreshToken refreshToken);

    void Remove(UserId userId);
}
