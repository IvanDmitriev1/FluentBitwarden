using BitwardenApi.Models;
using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.Modules.Account.Abstractions;

public interface IAccountProfileRepository
{
    AccountProfile[] GetAccounts();
    AccountProfile? GetById(UserId accountId);

    DateTimeOffset GetLastSyncTime(UserId accountId);
    void UpdateSyncTime(UserId accountId, DateTimeOffset syncTime);

    void Upsert(AccountProfile accountProfile);
    void Remove(UserId accountId);
}
