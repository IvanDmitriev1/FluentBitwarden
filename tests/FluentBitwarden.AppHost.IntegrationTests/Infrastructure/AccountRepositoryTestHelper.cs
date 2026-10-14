namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

internal static class AccountRepositoryTestHelper
{
    public static AccountProfile InsertAccount(AccountRepositoryTestDatabase database)
    {
        var account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "user@example.test",
            "first");
        InsertAccount(database, account);
        return account;
    }

    public static void InsertAccount(
        AccountRepositoryTestDatabase database,
        AccountProfile account)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        new AccountProfileRepository(unitOfWork).Upsert(account);
        unitOfWork.Commit();
    }

    public static void StoreToken(
        AccountRepositoryTestDatabase database,
        UserId userId,
        SessionRefreshToken token)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        new AccountBitwardenSessionTokenRepository(unitOfWork).Store(userId, token);
        unitOfWork.Commit();
    }

    public static void UpsertKeyMaterial(
        AccountRepositoryTestDatabase database,
        AccountKeyMaterial keyMaterial)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        new AccountKeyMaterialRepository(unitOfWork).Upsert(keyMaterial);
        unitOfWork.Commit();
    }

}
