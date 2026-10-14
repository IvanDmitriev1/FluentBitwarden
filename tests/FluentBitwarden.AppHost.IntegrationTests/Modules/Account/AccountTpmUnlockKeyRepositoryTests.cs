using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountTpmUnlockKeyRepositoryTests
{
    [Fact]
    public void Store_and_get_round_trip_protected_user_key_bytes()
    {
        using var database = new AccountRepositoryTestDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        byte[] expected = [0x00, 0x01, 0x7F, 0x80, 0xFE, 0xFF];

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountTpmUnlockKeyRepository(unitOfWork).Store(account.UserId, expected);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        byte[]? actual = new AccountTpmUnlockKeyRepository(readUnitOfWork).Get(account.UserId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Store_replaces_existing_protected_user_key()
    {
        using var database = new AccountRepositoryTestDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        byte[] initial = [0x10, 0x20, 0x30];
        byte[] replacement = [0x40, 0x50, 0x60, 0x70];

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountTpmUnlockKeyRepository(unitOfWork).Store(account.UserId, initial);
            unitOfWork.Commit();
        }

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountTpmUnlockKeyRepository(unitOfWork).Store(account.UserId, replacement);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        Assert.Equal(replacement, new AccountTpmUnlockKeyRepository(readUnitOfWork).Get(account.UserId));
    }

    [Fact]
    public void Get_returns_null_for_missing_key()
    {
        using var database = new AccountRepositoryTestDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);

        using var unitOfWork = database.CreateUnitOfWork();
        Assert.Null(new AccountTpmUnlockKeyRepository(unitOfWork).Get(account.UserId));
    }

    [Fact]
    public void Exists_is_false_before_storage_and_true_after_storage()
    {
        using var database = new AccountRepositoryTestDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);

        using (var unitOfWork = database.CreateUnitOfWork())
            Assert.False(new AccountTpmUnlockKeyRepository(unitOfWork).Exists(account.UserId));

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountTpmUnlockKeyRepository(unitOfWork).Store(account.UserId, [0x01, 0x02, 0x03]);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        Assert.True(new AccountTpmUnlockKeyRepository(readUnitOfWork).Exists(account.UserId));
    }

    [Fact]
    public void Remove_deletes_key_and_repeated_removal_is_harmless()
    {
        using var database = new AccountRepositoryTestDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountTpmUnlockKeyRepository(unitOfWork).Store(account.UserId, [0x01, 0x02, 0x03]);
            unitOfWork.Commit();
        }

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            var repository = new AccountTpmUnlockKeyRepository(unitOfWork);
            repository.Remove(account.UserId);
            repository.Remove(account.UserId);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        var repositoryAfterRemoval = new AccountTpmUnlockKeyRepository(readUnitOfWork);
        Assert.False(repositoryAfterRemoval.Exists(account.UserId));
        Assert.Null(repositoryAfterRemoval.Get(account.UserId));
    }

    [Fact]
    public void Get_uses_case_insensitive_user_id_lookup()
    {
        using var database = new AccountRepositoryTestDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        byte[] expected = [0x01, 0x80, 0xFF];
        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountTpmUnlockKeyRepository(unitOfWork).Store(account.UserId, expected);
            unitOfWork.Commit();
        }

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            using var command = unitOfWork.Connection.CreateCommand();
            command.CommandText = "UPDATE account_tpm_cng_unlock_keys SET user_id = upper(user_id);";
            command.Transaction = unitOfWork.Transaction;
            command.ExecuteNonQuery();
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        Assert.Equal(expected, new AccountTpmUnlockKeyRepository(readUnitOfWork).Get(account.UserId));
    }

    [Fact]
    public void Store_for_missing_account_fails_with_sqlite_foreign_key_error()
    {
        using var database = new AccountRepositoryTestDatabase();
        UserId missingUserId = UserId.Parse(AccountTestData.ThirdUserId);

        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();

        SqliteException exception = Assert.Throws<SqliteException>(() =>
            new AccountTpmUnlockKeyRepository(unitOfWork).Store(missingUserId, [0x01, 0x02, 0x03]));

        Assert.Contains("FOREIGN KEY constraint failed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Removing_account_profile_cascades_to_tpm_key()
    {
        using var database = new AccountRepositoryTestDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountTpmUnlockKeyRepository(unitOfWork).Store(account.UserId, [0x01, 0x02, 0x03]);
            unitOfWork.Commit();
        }

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Remove(account.UserId);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        var repository = new AccountTpmUnlockKeyRepository(readUnitOfWork);
        Assert.False(repository.Exists(account.UserId));
        Assert.Null(repository.Get(account.UserId));
    }
}
