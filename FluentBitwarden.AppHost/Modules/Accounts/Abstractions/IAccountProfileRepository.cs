using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Abstractions;

/// <summary>
/// Accounts' public repository surface, exposed on <c>UnitOfWork</c> so a sibling module can write
/// account state inside an ongoing transaction. This is the module's sibling-facing API: Vault's
/// sync uses <see cref="UpdateSyncedProfile"/> to persist the synced profile in the same
/// transaction as the rest of the sync.
/// </summary>
internal interface IAccountProfileRepository
{
    AccountProfile[] GetAccounts();

    AccountProfile? GetById(UserId accountId);

    AccountProfileDetails? GetProfileDetails(UserId accountId);

    void UpdateSyncedProfile(UserId accountId, VaultProfileResponse profile);

    void Upsert(AccountProfile accountProfile);

    void Remove(UserId accountId);
}
