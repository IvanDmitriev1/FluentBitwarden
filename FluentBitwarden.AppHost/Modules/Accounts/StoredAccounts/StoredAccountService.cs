using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts.Models;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts;

internal sealed class StoredAccountService(IUnitOfWorkFactory unitOfWorkFactory) : IStoredAccountStore
{
    public AccountProfile[] GetAccounts()
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        return unitOfWork.AccountProfileRepository.GetAccounts();
    }

    public AccountProfile? GetAccount(UserId userId)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        return unitOfWork.AccountProfileRepository.GetById(userId);
    }

    public AccountKeyMaterial? GetKeyMaterial(UserId userId)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        return unitOfWork.AccountKeyMaterialRepository.GetById(userId);
    }

    public void Save(AccountProfile profile, AccountKeyMaterial keyMaterial)
    {
        using var unitOfWork = unitOfWorkFactory.Create();

        unitOfWork.AccountProfileRepository.Upsert(profile);
        unitOfWork.AccountKeyMaterialRepository.Upsert(keyMaterial);

        unitOfWork.SaveChanges();
    }

    public void Remove(UserId userId)
    {
        using var unitOfWork = unitOfWorkFactory.Create();

        unitOfWork.AccountProfileRepository.Remove(userId);
        unitOfWork.AccountKeyMaterialRepository.Remove(userId);

        unitOfWork.SaveChanges();
    }
}