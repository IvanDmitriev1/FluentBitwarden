using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Account.Abstractions;

public interface IAccountRepository
{
    IReadOnlyList<StoredAccount> GetAccounts();
    StoredAccount? GetById(UserId accountId);
    void Upsert(StoredAccount account);
    void Remove(UserId accountId);
}