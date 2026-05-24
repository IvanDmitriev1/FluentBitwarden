using BitwardenApi.Models;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Account.Abstractions;

public interface IAccountKeyMaterialRepository
{
    AccountKeyMaterial? GetById(UserId userId);
    void Upsert(AccountKeyMaterial accountKeyMaterial);
}