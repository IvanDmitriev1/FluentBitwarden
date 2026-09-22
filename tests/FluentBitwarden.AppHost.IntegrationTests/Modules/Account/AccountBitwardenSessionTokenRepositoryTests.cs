using System.Text;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountBitwardenSessionTokenRepositoryTests(AccountRepositoryFixture fixture)
    : IClassFixture<AccountRepositoryFixture>
{
    [Fact]
    public void Store_and_get_round_trip_refresh_token()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        SessionRefreshToken expected = SessionRefreshToken.Parse("dummy-refresh-token");

        AccountRepositoryTestHelper.StoreToken(database, account.UserId, expected);

        SessionRefreshToken actual = AccountRepositoryTestHelper.ReadToken(database, account.UserId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Store_replaces_existing_refresh_token()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        SessionRefreshToken initial = SessionRefreshToken.Parse("dummy-refresh-token-initial");
        SessionRefreshToken replacement = SessionRefreshToken.Parse("dummy-refresh-token-replacement");

        AccountRepositoryTestHelper.StoreToken(database, account.UserId, initial);
        AccountRepositoryTestHelper.StoreToken(database, account.UserId, replacement);

        Assert.Equal(replacement, AccountRepositoryTestHelper.ReadToken(database, account.UserId));
    }

    [Fact]
    public void Store_protects_refresh_token_bytes_at_rest()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        const string tokenValue = "dummy-refresh-token-at-rest";
        AccountRepositoryTestHelper.StoreToken(database, account.UserId, SessionRefreshToken.Parse(tokenValue));

        using SqliteConnection connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT protected_refresh_token FROM account_session_tokens WHERE user_id = $userId;";
        command.Parameters.AddWithValue("$userId", account.UserId.ToString());
        byte[] protectedBytes = (byte[])command.ExecuteScalar()!;

        Assert.NotEmpty(protectedBytes);
        Assert.False(protectedBytes.AsSpan().SequenceEqual(Encoding.UTF8.GetBytes(tokenValue)));
    }

    [Fact]
    public void Remove_deletes_refresh_token()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        AccountRepositoryTestHelper.StoreToken(
            database,
            account.UserId,
            SessionRefreshToken.Parse("dummy-refresh-token"));

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountBitwardenSessionTokenRepository(unitOfWork).Remove(account.UserId);
            unitOfWork.Commit();
        }

        Assert.Equal(
            SessionRefreshToken.Empty,
            AccountRepositoryTestHelper.ReadToken(database, account.UserId));
    }

    [Fact]
    public void Missing_refresh_token_returns_empty_value()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);

        Assert.Equal(
            SessionRefreshToken.Empty,
            AccountRepositoryTestHelper.ReadToken(database, account.UserId));
    }

    [Fact]
    public void Removing_profile_cascades_to_refresh_token()
    {
        using var database = fixture.CreateDatabase();
        var account = AccountRepositoryTestHelper.InsertAccount(database);
        AccountRepositoryTestHelper.StoreToken(
            database,
            account.UserId,
            SessionRefreshToken.Parse("dummy-refresh-token"));

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Remove(account.UserId);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        readUnitOfWork.Begin();
        Assert.Equal(
            SessionRefreshToken.Empty,
            new AccountBitwardenSessionTokenRepository(readUnitOfWork).Get(account.UserId));
        readUnitOfWork.Commit();
    }
}
