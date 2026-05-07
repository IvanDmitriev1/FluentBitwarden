using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Account.Abstractions;

public interface IAccountProfileRepository
{
    IReadOnlyList<AccountProfile> GetAccounts();
    AccountProfile? GetById(UserId accountId);

    DateTimeOffset GetLastSyncTime(UserId accountId);
    void UpdateSyncTime(UserId accountId, DateTimeOffset syncTime);

    void Upsert(AccountProfile accountProfile);
    void Remove(UserId accountId);
}