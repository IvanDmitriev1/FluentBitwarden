namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

public sealed class UnitOfWorkTests(AccountRepositoryFixture fixture)
    : IClassFixture<AccountRepositoryFixture>
{
    [Fact]
    public void Commit_persists_transactional_repository_write()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "committed@example.test",
            "committed");

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Upsert(account);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        Assert.Equal(account, new AccountProfileRepository(readUnitOfWork).GetById(account.UserId));
    }

    [Fact]
    public void Disposing_without_commit_rolls_back_transactional_repository_write()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "rolled-back@example.test",
            "rolled-back");

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Upsert(account);
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        Assert.Null(new AccountProfileRepository(readUnitOfWork).GetById(account.UserId));
    }
}
