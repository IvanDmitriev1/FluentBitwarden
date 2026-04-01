using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Account.Abstractions;

public interface IAccountSecurityRepository
{
    Task<StoredAccountSecurity?> GetByAccountIdAsync(UserId accountId, CancellationToken cancellationToken = default);
    Task UpdateAsync(StoredAccountSecurity security, CancellationToken cancellationToken = default);
}
