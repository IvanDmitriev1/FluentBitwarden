using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Account.Repositories;

internal sealed class AccountRepository : IAccountRepository
{
    public Task<IReadOnlyList<StoredAccount>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<StoredAccount?> GetByIdAsync(UserId accountId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpsertAsync(StoredAccount account, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync(UserId accountId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}