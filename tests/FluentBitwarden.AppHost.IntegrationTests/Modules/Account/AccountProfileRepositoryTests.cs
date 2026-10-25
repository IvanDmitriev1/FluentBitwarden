using Dapper;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountProfileRepositoryTests
{
    [Fact]
    public void GetAccounts_returns_profiles_in_email_order()
    {
        using var database = new AccountRepositoryTestDatabase();
        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            var repository = new AccountProfileRepository(unitOfWork);
            repository.Upsert(AccountTestData.Profile(
                AccountTestData.FirstUserId,
                "zeta@example.test",
                "zeta"));
            repository.Upsert(AccountTestData.Profile(
                AccountTestData.SecondUserId,
                "alpha@example.test",
                "alpha"));
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        var accounts = new AccountProfileRepository(readUnitOfWork).GetAccounts();

        Assert.Equal(
            (string[])["alpha@example.test", "zeta@example.test"],
            accounts.Select(account => account.Email));
    }

    [Fact]
    public void GetById_uses_case_insensitive_user_id_lookup()
    {
        using var database = new AccountRepositoryTestDatabase();
        var expected = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "user@example.test",
            "first");

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Upsert(expected);
            unitOfWork.Commit();
        }

        using (SqliteConnection connection = database.OpenConnection())
        {
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE account_profiles SET user_id = upper(user_id);";
            command.ExecuteNonQuery();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        AccountProfile? actual = new AccountProfileRepository(readUnitOfWork)
            .GetById(expected.UserId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Upsert_inserts_then_updates_one_profile()
    {
        using var database = new AccountRepositoryTestDatabase();
        var initial = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "initial@example.test",
            "initial");
        var replacement = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "replacement@example.test",
            "replacement");

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            var repository = new AccountProfileRepository(unitOfWork);
            repository.Upsert(initial);
            unitOfWork.Commit();
        }

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Upsert(replacement);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        var repositoryAfterUpdate = new AccountProfileRepository(readUnitOfWork);

        Assert.Equal(replacement, repositoryAfterUpdate.GetById(initial.UserId));
        Assert.Single(repositoryAfterUpdate.GetAccounts());
    }

    [Fact]
    public void Missing_profile_and_details_return_null()
    {
        using var database = new AccountRepositoryTestDatabase();
        using var unitOfWork = database.CreateUnitOfWork();
        var repository = new AccountProfileRepository(unitOfWork);
        UserId missingUserId = UserId.Parse(AccountTestData.ThirdUserId);

        Assert.Null(repository.GetById(missingUserId));
        Assert.Null(repository.GetProfileDetails(missingUserId));
    }

    [Fact]
    public void GetProfileDetails_returns_null_when_unsynced_or_any_nullable_field_is_missing()
    {
        using var database = new AccountRepositoryTestDatabase();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        AccountRepositoryTestHelper.InsertAccount(database, account);
        long creationDateUnixMs = new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        using var connection = database.OpenConnection();
        Assert.Equal(1, connection.Execute(
            """
            UPDATE account_profiles
            SET profile_synced = 0, profile_name = @Name, profile_culture = @Culture,
                profile_creation_date_unix_ms = @CreationDateUnixMs
            WHERE user_id = @UserId;
            """,
            new
            {
                Name = "Ada",
                Culture = "en-US",
                CreationDateUnixMs = creationDateUnixMs,
                UserId = account.UserId.ToString()
            }));
        Assert.Null(ReadProfileDetails(database, account.UserId));

        Assert.Equal(1, connection.Execute(
            """
            UPDATE account_profiles
            SET profile_synced = 1, profile_name = NULL, profile_culture = @Culture,
                profile_creation_date_unix_ms = @CreationDateUnixMs
            WHERE user_id = @UserId;
            """,
            new { Culture = "en-US", CreationDateUnixMs = creationDateUnixMs, UserId = account.UserId.ToString() }));
        Assert.Null(ReadProfileDetails(database, account.UserId));

        Assert.Equal(1, connection.Execute(
            """
            UPDATE account_profiles
            SET profile_name = @Name, profile_culture = NULL
            WHERE user_id = @UserId;
            """,
            new { Name = "Ada", UserId = account.UserId.ToString() }));
        Assert.Null(ReadProfileDetails(database, account.UserId));

        Assert.Equal(1, connection.Execute(
            """
            UPDATE account_profiles
            SET profile_culture = @Culture, profile_creation_date_unix_ms = NULL
            WHERE user_id = @UserId;
            """,
            new { Culture = "en-US", UserId = account.UserId.ToString() }));
        Assert.Null(ReadProfileDetails(database, account.UserId));
    }

    [Fact]
    public void GetProfileDetails_maps_complete_synced_metadata_from_a_fresh_sqlite_connection()
    {
        using var database = new AccountRepositoryTestDatabase();
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        AccountRepositoryTestHelper.InsertAccount(database, account);
        DateTimeOffset profileCreatedAt = new(2024, 5, 1, 2, 3, 4, 567, TimeSpan.FromHours(3));
        long creationDateUnixMs = profileCreatedAt.ToUnixTimeMilliseconds();

        using (SqliteConnection connection = database.OpenConnection())
        {
            Assert.Equal(1, connection.Execute(
                """
                UPDATE account_profiles
                SET profile_synced = 1, profile_name = @Name, profile_culture = @Culture,
                    profile_creation_date_unix_ms = @CreationDateUnixMs
                WHERE user_id = @UserId;
                """,
                new
                {
                    Name = "Ada Lovelace",
                    Culture = "en-GB",
                    CreationDateUnixMs = creationDateUnixMs,
                    UserId = account.UserId.ToString()
                }));
        }

        Assert.Equal(
            new AccountProfileDetails("Ada Lovelace", "en-GB", DateTimeOffset.FromUnixTimeMilliseconds(creationDateUnixMs)),
            ReadProfileDetails(database, account.UserId));
    }

    [Fact]
    public void Remove_deletes_profile()
    {
        using var database = new AccountRepositoryTestDatabase();
        var account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "user@example.test",
            "first");

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Upsert(account);
            unitOfWork.Commit();
        }

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Remove(account.UserId);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        Assert.Null(new AccountProfileRepository(readUnitOfWork).GetById(account.UserId));
    }

    private AccountProfileDetails? ReadProfileDetails(AccountRepositoryTestDatabase database, UserId userId)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        return new AccountProfileRepository(unitOfWork).GetProfileDetails(userId);
    }
}
