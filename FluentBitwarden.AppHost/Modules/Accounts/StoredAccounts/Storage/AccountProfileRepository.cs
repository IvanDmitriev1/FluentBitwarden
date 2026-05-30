using BitwardenApi.Models;
using Dapper;
using FluentBitwarden.Contracts.Session.Models;
using FluentBitwarden.Data;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts.Storage;

internal sealed class AccountProfileRepository(SqliteTransaction transaction) : BaseRepository(transaction)
{
    internal readonly record struct AccountProfileRow(
        string UserId,
        string Email,
        string ApiBase,
        string IdentityBase,
        string NotificationsBase,
        string VaultBase,
        long LastSyncAtUnixMs);

    public AccountProfile[] GetAccounts()
    {
        const string sql = """
                           SELECT
                               user_id,
                               email,
                               api_base,
                               identity_base,
                               notifications_base,
                               vault_base,
                               last_sync_at_unix_ms
                           FROM account_profiles
                           ORDER BY last_sync_at_unix_ms DESC;
                           """;

        var rows = Connection.Query<AccountProfileRow>(
            sql,
            transaction: Transaction);

        return rows.Select(static row => MapToDomain(row)).ToArray();
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
                               vault_base,
                               last_sync_at_unix_ms
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

    /*public void SetUnlockMethods(UserId accountId, UnlockMethodType availableUnlockMethods)
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
    */

    public void Upsert(AccountProfile accountProfile)
    {
        const string sql = """
                           INSERT INTO account_profiles (
                               user_id,
                               email,
                               api_base,
                               identity_base,
                               notifications_base,
                               vault_base,
                               last_sync_at_unix_ms
                           )
                           VALUES (
                               @UserId,
                               @Email,
                               @ApiBase,
                               @IdentityBase,
                               @NotificationsBase,
                               @VaultBase,
                               @LastSyncAtUnixMs
                           )
                           ON CONFLICT(user_id) DO UPDATE SET
                               email                = excluded.email,
                               api_base             = excluded.api_base,
                               identity_base        = excluded.identity_base,
                               notifications_base   = excluded.notifications_base,
                               vault_base           = excluded.vault_base,
                               last_sync_at_unix_ms = excluded.last_sync_at_unix_ms
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
                VaultBase = accountProfile.Environment.VaultBase.ToString(),
                LastSyncAtUnixMs = accountProfile.LastSyncAt.ToUnixTimeMilliseconds()
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
            NotificationsBase: new Uri(row.NotificationsBase, UriKind.Absolute),
            VaultBase: new Uri(row.VaultBase, UriKind.Absolute)),
        LastSyncAt: DateTimeOffset.FromUnixTimeMilliseconds(row.LastSyncAtUnixMs));
}
