using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using Dapper;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using FluentBitwarden.Modules.Session.Models;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace FluentBitwarden.Modules.Account.Repositories;

internal sealed class AccountProfileRepository(SqliteTransaction transaction)
    : BaseRepository(transaction), IAccountProfileRepository
{
    private readonly record struct AccountProfileRow(
        string UserId,
        string Email,
        string ApiBase,
        string IdentityBase,
        string NotificationsBase,
        long LastSyncAtUnixMs,
        byte AvailableUnlockMethods);

    public IReadOnlyList<AccountProfile> GetAccounts()
    {
        const string sql = """
                           SELECT
                               user_id,
                               email,
                               api_base,
                               identity_base,
                               notifications_base,
                               last_sync_at_unix_ms,
                               available_unlock_methods
                           FROM account_profiles
                           ORDER BY last_sync_at_unix_ms DESC;
                           """;

        var rows = Connection.Query<AccountProfileRow>(
            sql,
            transaction: Transaction);

        return rows.Select(static row => MapToDomain(row)).ToList();
    }

    public AccountProfile? GetById(UserId accountId)
    {
        const string sql = """
                           SELECT
                               user_id,
                               email,
                               api_base,
                               identity_base,
                               notifications_base,
                               last_sync_at_unix_ms,
                               available_unlock_methods
                           FROM account_profiles
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        var row = Connection.QueryFirstOrDefault<AccountProfileRow>(
            sql,
            new
            {
                UserId = accountId.ToString()
            },
            transaction: Transaction);

        return row == default ? null : MapToDomain(row);
    }

    public DateTimeOffset GetLastSyncTime(UserId accountId)
    {
        const string sql = """
                           SELECT last_sync_at_unix_ms
                           FROM account_profiles
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        var lastSyncUnixMs = Connection.QuerySingleOrDefault<long?>(
            sql,
            new
            {
                UserId = accountId.ToString()
            },
            transaction: Transaction);

        if (lastSyncUnixMs is null)
        {
            throw new InvalidOperationException($"Account profile was not found for user '{accountId}'.");
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(lastSyncUnixMs.Value);
    }

    public void UpdateSyncTime(UserId accountId, DateTimeOffset syncTime)
    {
        const string sql = """
                           UPDATE account_profiles
                           SET last_sync_at_unix_ms = @LastSyncAtUnixMs
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        Connection.Execute(
            sql,
            new
            {
                UserId = accountId.ToString(),
                LastSyncAtUnixMs = syncTime.ToUnixTimeMilliseconds()
            },
            transaction: Transaction);
    }

    public void SetUnlockMethods(UserId accountId, UnlockMethodType availableUnlockMethods)
    {
        const string sql = """
                           UPDATE account_profiles
                           SET available_unlock_methods = @AvailableUnlockMethods
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        Connection.Execute(
            sql,
            new
            {
                UserId = accountId.ToString(),
                AvailableUnlockMethods = (byte)availableUnlockMethods
            },
            transaction: Transaction);
    }

    public void Upsert(AccountProfile accountProfile)
    {
        const string sql = """
                           INSERT INTO account_profiles (
                               user_id,
                               email,
                               api_base,
                               identity_base,
                               notifications_base,
                               last_sync_at_unix_ms,
                               available_unlock_methods
                           )
                           VALUES (
                               @UserId,
                               @Email,
                               @ApiBase,
                               @IdentityBase,
                               @NotificationsBase,
                               @LastSyncAtUnixMs,
                               @AvailableUnlockMethods
                           )
                           ON CONFLICT(user_id) DO UPDATE SET
                               email                = excluded.email,
                               api_base             = excluded.api_base,
                               identity_base        = excluded.identity_base,
                               notifications_base   = excluded.notifications_base,
                               last_sync_at_unix_ms = excluded.last_sync_at_unix_ms,
                               available_unlock_methods = excluded.available_unlock_methods;
                           """;

        Connection.Execute(
            sql,
            new
            {
                UserId = accountProfile.UserId.ToString(),
                Email = accountProfile.Email,
                ApiBase = accountProfile.Environment.ApiBase.ToString(),
                IdentityBase = accountProfile.Environment.IdentityBase.ToString(),
                NotificationsBase = accountProfile.Environment.NotificationsBase.ToString(),
                LastSyncAtUnixMs = accountProfile.LastSyncAt.ToUnixTimeMilliseconds(),
                AvailableUnlockMethods = (byte)accountProfile.AvailableUnlockMethods
            },
            transaction: Transaction);
    }

    public void Remove(UserId accountId)
    {
        const string sql = """
                           DELETE FROM account_profiles
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        Connection.Execute(
            sql,
            new
            {
                UserId = accountId.ToString()
            },
            transaction: Transaction);
    }

    private static AccountProfile MapToDomain(in AccountProfileRow row) => new(
        UserId: UserId.Parse(row.UserId),
        Email: row.Email,
        Environment: new BitwardenEnvironment(
            ApiBase: new Uri(row.ApiBase, UriKind.Absolute),
            IdentityBase: new Uri(row.IdentityBase, UriKind.Absolute),
            NotificationsBase: new Uri(row.NotificationsBase, UriKind.Absolute)),
        LastSyncAt: DateTimeOffset.FromUnixTimeMilliseconds(row.LastSyncAtUnixMs),
        AvailableUnlockMethods: (UnlockMethodType)row.AvailableUnlockMethods);
}
