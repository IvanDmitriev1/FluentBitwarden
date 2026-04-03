using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Account.Abstractions;

public interface IAccountDecryptionRepository
{
    AccountDecryption? GetById(UserId userId);
    void Upsert(AccountDecryption accountDecryption);
}