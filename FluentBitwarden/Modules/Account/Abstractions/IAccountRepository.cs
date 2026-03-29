using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Account.Abstractions;

public interface IAccountRepository
{
    Task<IReadOnlyList<StoredAccount>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<StoredAccount?> GetByIdAsync(UserId accountId, CancellationToken cancellationToken = default);
    Task UpsertAsync(StoredAccount account, CancellationToken cancellationToken = default);
    Task RemoveAsync(UserId accountId, CancellationToken cancellationToken = default);
}