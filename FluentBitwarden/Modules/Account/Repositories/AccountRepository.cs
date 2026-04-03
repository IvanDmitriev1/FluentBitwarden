using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using Dapper;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace FluentBitwarden.Modules.Account.Repositories;

internal sealed class AccountRepository(SqliteTransaction transaction) : BaseRepository(transaction), IAccountRepository
{
    public readonly record struct AccountRow(
        string UserId,
        string Email,
        string ApiBase,
        string IdentityBase,
        string NotificationsBase,
        long LastSyncAtUnixMs);

    private static StoredAccount MapToDomain(AccountRow row) => new(
        UserId: UserId.Parse(row.UserId), 
        Email: row.Email,
        Environment: new BitwardenEnvironment(
            ApiBase: new Uri(row.ApiBase, UriKind.Absolute),
            IdentityBase: new Uri(row.IdentityBase, UriKind.Absolute),
            NotificationsBase: new Uri(row.NotificationsBase, UriKind.Absolute)),
        LastSyncAt: DateTimeOffset.FromUnixTimeMilliseconds(row.LastSyncAtUnixMs));

    public IReadOnlyList<StoredAccount> GetAccounts()
    {
        const string sql = """
                           SELECT *
                           FROM accounts
                           ORDER BY last_sync_at_unix_ms DESC;
                           """;

        var rows = Connection.Query<AccountRow>(sql, transaction: Transaction);
        return rows.Select(MapToDomain).ToList();
    }

    public StoredAccount? GetById(UserId accountId)
    {
        const string sql = """
                           SELECT *
                           FROM accounts
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        var row = Connection.QueryFirstOrDefault<AccountRow>(sql,
            new
            {
                UserId = accountId.ToString()
            }, transaction: Transaction);

        return row == default ? null : MapToDomain(row);
    }

    public DateTimeOffset GetLastSyncTime(UserId accountId)
    {
        const string sql = """
                           SELECT last_sync_at_unix_ms
                           FROM accounts
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        var lastSyncUtc = Connection.QueryFirstOrDefault<Int64>(sql, new
        {
            UserId = accountId.ToString()
        }, transaction: Transaction);

        return DateTimeOffset.FromUnixTimeMilliseconds(lastSyncUtc);
    }

    public void UpdateSyncTime(UserId accountId, DateTimeOffset syncTime)
    {
        const string sql = """
                           UPDATE accounts
                           SET last_sync_at_unix_ms = @LastSyncAtUnixMs
                           WHERE user_id = @UserId;
                           """;

        Connection.Execute(sql, new
        {
            UserId = accountId.ToString(),
            LastSyncAtUnixMs = syncTime.ToUnixTimeMilliseconds(),
        }, transaction: Transaction);
    }

    public void Upsert(StoredAccount account)
    {
        const string sql = """
                           INSERT INTO accounts (user_id, email, api_base, identity_base, notifications_base, last_sync_at_unix_ms)
                           VALUES (@UserId, @Email, @ApiBase, @IdentityBase, @NotificationsBase, @LastSyncAtUnixMs)
                           ON CONFLICT(user_id) DO UPDATE SET
                               email                = excluded.email,
                               api_base             = excluded.api_base,
                               identity_base        = excluded.identity_base,
                               notifications_base   = excluded.notifications_base,
                               last_sync_at_unix_ms = excluded.last_sync_at_unix_ms;
                           """;

        Connection.Execute(sql, new
        {
            UserId = account.UserId.ToString(),
            Email = account.Email,
            ApiBase = account.Environment.ApiBase.ToString(),
            IdentityBase = account.Environment.IdentityBase.ToString(),
            NotificationsBase = account.Environment.NotificationsBase.ToString(),
            LastSyncAtUnixMs = account.LastSyncAt.ToUnixTimeMilliseconds(),
        }, transaction: Transaction);
    }

    public void Remove(UserId accountId)
    {
        const string sql = "DELETE FROM accounts WHERE user_id = @UserId;";

        Connection.Execute(sql,
            new
            {
                UserId = accountId
            }, transaction: Transaction);

    }
}
