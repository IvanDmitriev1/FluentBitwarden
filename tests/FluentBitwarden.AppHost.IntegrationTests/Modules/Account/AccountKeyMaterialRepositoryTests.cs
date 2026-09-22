using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountKeyMaterialRepositoryTests(AccountRepositoryFixture fixture)
    : IClassFixture<AccountRepositoryFixture>
{
    [Fact]
    public void Upsert_round_trips_pbkdf2_material()
    {
        var keyMaterial = AccountTestData.KeyMaterial(
            AccountTestData.FirstUserId,
            new KdfConfig.Pbkdf2(600_000),
            "pbkdf2");

        AccountKeyMaterial actual = RoundTrip(keyMaterial);

        Assert.Equal(keyMaterial, actual);
    }

    [Fact]
    public void Upsert_round_trips_argon2id_material()
    {
        var keyMaterial = AccountTestData.KeyMaterial(
            AccountTestData.FirstUserId,
            new KdfConfig.Argon2Id(3, 64, 4),
            "argon2id");

        AccountKeyMaterial actual = RoundTrip(keyMaterial);

        Assert.Equal(keyMaterial, actual);
    }

    [Fact]
    public void Upsert_replaces_existing_material()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "user@example.test",
            "first");
        var initial = AccountTestData.KeyMaterial(
            AccountTestData.FirstUserId,
            new KdfConfig.Pbkdf2(600_000),
            "initial");
        var replacement = AccountTestData.KeyMaterial(
            AccountTestData.FirstUserId,
            new KdfConfig.Argon2Id(3, 64, 4),
            "replacement");

        AccountRepositoryTestHelper.InsertAccount(database, account);
        AccountRepositoryTestHelper.UpsertKeyMaterial(database, initial);
        AccountRepositoryTestHelper.UpsertKeyMaterial(database, replacement);

        using var unitOfWork = database.CreateUnitOfWork();
        Assert.Equal(
            replacement,
            new AccountKeyMaterialRepository(unitOfWork).GetById(account.UserId));
    }

    [Fact]
    public void Missing_material_returns_null_and_remove_is_idempotent()
    {
        using var database = fixture.CreateDatabase();
        using var unitOfWork = database.CreateUnitOfWork();
        var repository = new AccountKeyMaterialRepository(unitOfWork);
        UserId missingUserId = UserId.Parse(AccountTestData.ThirdUserId);

        Assert.Null(repository.GetById(missingUserId));

        unitOfWork.Begin();
        repository.Remove(missingUserId);
        unitOfWork.Commit();
    }

    [Fact]
    public void Remove_deletes_material()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "user@example.test",
            "first");
        var keyMaterial = AccountTestData.KeyMaterial(
            AccountTestData.FirstUserId,
            new KdfConfig.Pbkdf2(600_000),
            "remove");

        AccountRepositoryTestHelper.InsertAccount(database, account);
        AccountRepositoryTestHelper.UpsertKeyMaterial(database, keyMaterial);

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountKeyMaterialRepository(unitOfWork).Remove(account.UserId);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        Assert.Null(new AccountKeyMaterialRepository(readUnitOfWork).GetById(account.UserId));
    }

    [Fact]
    public void Upsert_rejects_material_for_missing_profile()
    {
        using var database = fixture.CreateDatabase();
        var keyMaterial = AccountTestData.KeyMaterial(
            AccountTestData.ThirdUserId,
            new KdfConfig.Pbkdf2(600_000),
            "orphan");

        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();

        Assert.Throws<SqliteException>(() =>
            new AccountKeyMaterialRepository(unitOfWork).Upsert(keyMaterial));
    }

    [Fact]
    public void Removing_profile_cascades_to_key_material()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "user@example.test",
            "first");
        var keyMaterial = AccountTestData.KeyMaterial(
            AccountTestData.FirstUserId,
            new KdfConfig.Pbkdf2(600_000),
            "cascade");

        AccountRepositoryTestHelper.InsertAccount(database, account);
        AccountRepositoryTestHelper.UpsertKeyMaterial(database, keyMaterial);

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Remove(account.UserId);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        Assert.Null(new AccountKeyMaterialRepository(readUnitOfWork).GetById(account.UserId));
    }

    private AccountKeyMaterial RoundTrip(AccountKeyMaterial keyMaterial)
    {
        using var database = fixture.CreateDatabase();
        AccountRepositoryTestHelper.InsertAccount(database, AccountTestData.Profile(
            keyMaterial.UserId.ToString(),
            "user@example.test",
            "first"));
        AccountRepositoryTestHelper.UpsertKeyMaterial(database, keyMaterial);

        using var unitOfWork = database.CreateUnitOfWork();
        return new AccountKeyMaterialRepository(unitOfWork).GetById(keyMaterial.UserId)!;
    }

}
