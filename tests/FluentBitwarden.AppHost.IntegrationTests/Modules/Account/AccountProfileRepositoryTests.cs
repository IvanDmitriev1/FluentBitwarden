using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountProfileRepositoryTests(AccountRepositoryFixture fixture)
    : IClassFixture<AccountRepositoryFixture>
{
    [Fact]
    public void GetAccounts_returns_profiles_in_email_order()
    {
        using var database = fixture.CreateDatabase();
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
            ["alpha@example.test", "zeta@example.test"],
            accounts.Select(account => account.Email));
    }

    [Fact]
    public void GetById_uses_case_insensitive_user_id_lookup()
    {
        using var database = fixture.CreateDatabase();
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
        using var database = fixture.CreateDatabase();
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
    public void UpdateSyncedProfile_persists_details_and_updates_email()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "before@example.test",
            "first");
        DateTimeOffset creationDate = AccountTestData.ProfileCreationDate;
        var syncedProfile = AccountTestData.SyncedProfile(
            AccountTestData.FirstUserId,
            "after@example.test",
            "Test User",
            "en-US",
            creationDate);

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            var repository = new AccountProfileRepository(unitOfWork);
            repository.Upsert(account);
            unitOfWork.Commit();
        }

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork)
                .UpdateSyncedProfile(account.UserId, syncedProfile);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        var repositoryAfterSync = new AccountProfileRepository(readUnitOfWork);

        Assert.Equal("after@example.test", repositoryAfterSync.GetById(account.UserId)?.Email);
        Assert.Equal(
            new AccountProfileDetails("Test User", "en-US", creationDate),
            repositoryAfterSync.GetProfileDetails(account.UserId));
    }

    [Fact]
    public void Missing_profile_and_details_return_null()
    {
        using var database = fixture.CreateDatabase();
        using var unitOfWork = database.CreateUnitOfWork();
        var repository = new AccountProfileRepository(unitOfWork);
        UserId missingUserId = UserId.Parse(AccountTestData.ThirdUserId);

        Assert.Null(repository.GetById(missingUserId));
        Assert.Null(repository.GetProfileDetails(missingUserId));
    }

    [Fact]
    public void Remove_deletes_profile()
    {
        using var database = fixture.CreateDatabase();
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
}
