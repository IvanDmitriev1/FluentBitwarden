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
        var account = InsertAccount(database);
        RefreshToken expected = RefreshToken.Parse("dummy-refresh-token");

        StoreToken(database, account.UserId, expected);

        RefreshToken actual = ReadToken(database, account.UserId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Store_replaces_existing_refresh_token()
    {
        using var database = fixture.CreateDatabase();
        var account = InsertAccount(database);
        RefreshToken initial = RefreshToken.Parse("dummy-refresh-token-initial");
        RefreshToken replacement = RefreshToken.Parse("dummy-refresh-token-replacement");

        StoreToken(database, account.UserId, initial);
        StoreToken(database, account.UserId, replacement);

        Assert.Equal(replacement, ReadToken(database, account.UserId));
    }

    [Fact]
    public void Store_protects_refresh_token_bytes_at_rest()
    {
        using var database = fixture.CreateDatabase();
        var account = InsertAccount(database);
        const string tokenValue = "dummy-refresh-token-at-rest";
        StoreToken(database, account.UserId, RefreshToken.Parse(tokenValue));

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
        var account = InsertAccount(database);
        StoreToken(database, account.UserId, RefreshToken.Parse("dummy-refresh-token"));

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountBitwardenSessionTokenRepository(unitOfWork).Remove(account.UserId);
            unitOfWork.Commit();
        }

        Assert.Equal(RefreshToken.Empty, ReadToken(database, account.UserId));
    }

    [Fact]
    public void Missing_refresh_token_returns_empty_value()
    {
        using var database = fixture.CreateDatabase();
        var account = InsertAccount(database);

        Assert.Equal(RefreshToken.Empty, ReadToken(database, account.UserId));
    }

    [Fact]
    public void Removing_profile_cascades_to_refresh_token()
    {
        using var database = fixture.CreateDatabase();
        var account = InsertAccount(database);
        StoreToken(database, account.UserId, RefreshToken.Parse("dummy-refresh-token"));

        using (var unitOfWork = database.CreateUnitOfWork())
        {
            unitOfWork.Begin();
            new AccountProfileRepository(unitOfWork).Remove(account.UserId);
            unitOfWork.Commit();
        }

        using var readUnitOfWork = database.CreateUnitOfWork();
        readUnitOfWork.Begin();
        Assert.Equal(
            RefreshToken.Empty,
            new AccountBitwardenSessionTokenRepository(readUnitOfWork).Get(account.UserId));
        readUnitOfWork.Commit();
    }

    private static AccountProfile InsertAccount(AccountRepositoryTestDatabase database)
    {
        var account = AccountTestData.Profile(
            AccountTestData.FirstUserId,
            "user@example.test",
            "first");
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        new AccountProfileRepository(unitOfWork).Upsert(account);
        unitOfWork.Commit();
        return account;
    }

    private static void StoreToken(
        AccountRepositoryTestDatabase database,
        UserId userId,
        RefreshToken token)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        new AccountBitwardenSessionTokenRepository(unitOfWork).Store(userId, token);
        unitOfWork.Commit();
    }

    private static RefreshToken ReadToken(
        AccountRepositoryTestDatabase database,
        UserId userId)
    {
        using var unitOfWork = database.CreateUnitOfWork();
        unitOfWork.Begin();
        RefreshToken token = new AccountBitwardenSessionTokenRepository(unitOfWork).Get(userId);
        unitOfWork.Commit();
        return token;
    }
}
