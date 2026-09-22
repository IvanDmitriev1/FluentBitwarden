using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountTpmUnlockKeyRepositoryTests(AccountRepositoryFixture fixture)
    : IClassFixture<AccountRepositoryFixture>
{
    [Fact]
    public void Store_and_get_round_trip_protected_user_key_bytes()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        byte[] expected = [0x00, 0x01, 0x7F, 0x80, 0xFE, 0xFF];

        AccountRepositoryTestHelper.StoreKey(database, account.UserId, expected);

        byte[]? actual = AccountRepositoryTestHelper.GetKey(database, account.UserId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Store_replaces_existing_protected_user_key()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        byte[] initial = [0x10, 0x20, 0x30];
        byte[] replacement = [0x40, 0x50, 0x60, 0x70];

        AccountRepositoryTestHelper.StoreKey(database, account.UserId, initial);
        AccountRepositoryTestHelper.StoreKey(database, account.UserId, replacement);

        Assert.Equal(replacement, AccountRepositoryTestHelper.GetKey(database, account.UserId));
    }

    [Fact]
    public void Get_returns_null_for_missing_key()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);

        Assert.Null(AccountRepositoryTestHelper.GetKey(database, account.UserId));
    }

    [Fact]
    public void Exists_is_false_before_storage_and_true_after_storage()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);

        Assert.False(AccountRepositoryTestHelper.KeyExists(database, account.UserId));

        AccountRepositoryTestHelper.StoreKey(database, account.UserId, [0x01, 0x02, 0x03]);

        Assert.True(AccountRepositoryTestHelper.KeyExists(database, account.UserId));
    }

    [Fact]
    public void Remove_deletes_key_and_repeated_removal_is_harmless()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        AccountRepositoryTestHelper.StoreKey(database, account.UserId, [0x01, 0x02, 0x03]);

        AccountRepositoryTestHelper.RemoveKey(database, account.UserId);
        AccountRepositoryTestHelper.RemoveKey(database, account.UserId);

        Assert.False(AccountRepositoryTestHelper.KeyExists(database, account.UserId));
        Assert.Null(AccountRepositoryTestHelper.GetKey(database, account.UserId));
    }

    [Fact]
    public void Get_uses_case_insensitive_user_id_lookup()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        byte[] expected = [0x01, 0x80, 0xFF];
        AccountRepositoryTestHelper.StoreKey(database, account.UserId, expected);

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            using var command = unitOfWork.Connection.CreateCommand();
            command.CommandText = "UPDATE account_tpm_cng_unlock_keys SET user_id = upper(user_id);";
            command.Transaction = unitOfWork.Transaction;
            command.ExecuteNonQuery();
            unitOfWork.Commit();
        }

        Assert.Equal(
            expected,
            AccountRepositoryTestHelper.GetKey(database, account.UserId));
    }

    [Fact]
    public void Store_for_missing_account_fails_with_sqlite_foreign_key_error()
    {
        using var database = fixture.CreateDatabase();
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
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        AccountRepositoryTestHelper.StoreKey(database, account.UserId, [0x01, 0x02, 0x03]);

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Remove(account.UserId);
            unitOfWork.Commit();
        }

        Assert.False(AccountRepositoryTestHelper.KeyExists(database, account.UserId));
        Assert.Null(AccountRepositoryTestHelper.GetKey(database, account.UserId));
    }
}
