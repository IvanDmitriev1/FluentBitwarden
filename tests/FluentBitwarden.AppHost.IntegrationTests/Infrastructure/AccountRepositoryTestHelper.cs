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

    public static SessionRefreshToken ReadToken(
        AccountRepositoryTestDatabase database,
        UserId userId)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        SessionRefreshToken token = new AccountBitwardenSessionTokenRepository(unitOfWork).Get(userId);
        unitOfWork.Commit();
        return token;
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

    public static void StoreKey(
        AccountRepositoryTestDatabase database,
        UserId userId,
        byte[] protectedUserKey)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        new AccountTpmUnlockKeyRepository(unitOfWork).Store(userId, protectedUserKey);
        unitOfWork.Commit();
    }

    public static byte[]? GetKey(
        AccountRepositoryTestDatabase database,
        UserId userId)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        byte[]? protectedUserKey = new AccountTpmUnlockKeyRepository(unitOfWork).Get(userId);
        unitOfWork.Commit();
        return protectedUserKey;
    }

    public static bool KeyExists(
        AccountRepositoryTestDatabase database,
        UserId userId)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        bool exists = new AccountTpmUnlockKeyRepository(unitOfWork).Exists(userId);
        unitOfWork.Commit();
        return exists;
    }

    public static void RemoveKey(
        AccountRepositoryTestDatabase database,
        UserId userId)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        new AccountTpmUnlockKeyRepository(unitOfWork).Remove(userId);
        unitOfWork.Commit();
    }
}
