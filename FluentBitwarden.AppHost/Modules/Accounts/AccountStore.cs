using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts;

internal sealed class AccountStore(IUnitOfWorkFactory unitOfWorkFactory) : IAccountStore
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

    public AccountProfileDetails? GetAccountProfileDetails(UserId userId)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        return unitOfWork.AccountProfileRepository.GetProfileDetails(userId);
    }

    public AccountKeyMaterial? GetKeyMaterial(UserId userId)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        return unitOfWork.AccountKeyMaterialRepository.GetById(userId);
    }

    public RefreshToken GetRefreshToken(UserId userId)
    {
        using var unitOfWork = unitOfWorkFactory.Create();
        return unitOfWork.RefreshTokenRepository.Get(userId);
    }

    public void Save(AccountProfile profile, AccountKeyMaterial keyMaterial)
    {
        using var unitOfWork = unitOfWorkFactory.Create();

        unitOfWork.AccountProfileRepository.Upsert(profile);
        unitOfWork.AccountKeyMaterialRepository.Upsert(keyMaterial);

        unitOfWork.SaveChanges();
    }

    public void SaveAuthenticatedAccount(
        AccountProfile profile,
        AccountKeyMaterial keyMaterial,
        RefreshToken refreshToken)
    {
        using var unitOfWork = unitOfWorkFactory.Create();

        unitOfWork.AccountProfileRepository.Upsert(profile);
        unitOfWork.AccountKeyMaterialRepository.Upsert(keyMaterial);
        unitOfWork.RefreshTokenRepository.Store(profile.UserId, refreshToken);

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
