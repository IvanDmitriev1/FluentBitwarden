using Dapper;
using FluentBitwarden.AppHost.Data.Abstractions;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Modules.Accounts.Persistence;

internal sealed class AccountProfileRepository(SqliteTransaction transaction) : BaseRepository(transaction)
{
    internal sealed record AccountProfileRow(
        string UserId,
        string Email,
        string ApiBase,
        string IdentityBase,
        string NotificationsBase,
        string VaultBase,
        long LastSyncAtUnixMs);

    internal sealed record AccountProfileDetailsRow(
        string? ProfileName,
        string? ProfileCulture,
        long? ProfileCreationDateUnixMs,
        int ProfileSynced);

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

        AccountProfileRow? row = Connection.QueryFirstOrDefault<AccountProfileRow>(
            sql,
            new
            {
                UserId = accountId.ToString()
            },
            transaction: Transaction);

        return row is null ? null : MapToDomain(row);
    }

    public AccountProfileDetails? GetProfileDetails(UserId accountId)
    {
        const string sql = """
                           SELECT
                               profile_name,
                               profile_culture,
                               profile_creation_date_unix_ms,
                               profile_synced
                           FROM account_profiles
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        AccountProfileDetailsRow? row = Connection.QueryFirstOrDefault<AccountProfileDetailsRow>(
            sql,
            new
            {
                UserId = accountId.ToString()
            },
            transaction: Transaction);

        return row is null ? null : MapProfileDetails(row);
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
            throw new InvalidOperationException($"Account profile was not found for user '{accountId}'.");

        return DateTimeOffset.FromUnixTimeMilliseconds(lastSyncUnixMs.Value);
    }

    public void UpdateSyncedProfile(
        UserId accountId,
        VaultProfileDto profile,
        DateTimeOffset syncTime)
    {
        const string sql = """
                           UPDATE account_profiles
                           SET
                               email                         = @Email,
                               last_sync_at_unix_ms          = @LastSyncAtUnixMs,
                               profile_name                  = @ProfileName,
                               profile_culture               = @ProfileCulture,
                               profile_creation_date_unix_ms = @ProfileCreationDateUnixMs,
                               profile_synced                = 1
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        var affectedRows = Connection.Execute(
            sql,
            new
            {
                UserId = accountId.ToString(),
                Email = profile.Email,
                LastSyncAtUnixMs = syncTime.ToUnixTimeMilliseconds(),
                ProfileName = NullIfWhiteSpace(profile.Name),
                ProfileCulture = NullIfWhiteSpace(profile.Culture),
                ProfileCreationDateUnixMs = profile.CreationDate.ToUnixTimeMilliseconds()
            },
            transaction: Transaction);

        if (affectedRows == 0)
            throw new InvalidOperationException($"Account profile was not found for user '{accountId}'.");
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
                               email                         = excluded.email,
                               api_base                      = excluded.api_base,
                               identity_base                 = excluded.identity_base,
                               notifications_base            = excluded.notifications_base,
                               vault_base                    = excluded.vault_base,
                               last_sync_at_unix_ms          = excluded.last_sync_at_unix_ms;
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

    private static AccountProfile MapToDomain(AccountProfileRow row) => new(
        UserId: UserId.Parse(row.UserId),
        Email: row.Email,
        Environment: new BitwardenEnvironment(
            ApiBase: new Uri(row.ApiBase, UriKind.Absolute),
            IdentityBase: new Uri(row.IdentityBase, UriKind.Absolute),
            NotificationsBase: new Uri(row.NotificationsBase, UriKind.Absolute),
            VaultBase: new Uri(row.VaultBase, UriKind.Absolute)),
        LastSyncAt: DateTimeOffset.FromUnixTimeMilliseconds(row.LastSyncAtUnixMs));

    private static AccountProfileDetails? MapProfileDetails(AccountProfileDetailsRow row)
    {
        if (row.ProfileSynced == 0)
            return null;

        return new AccountProfileDetails(
            Name: row.ProfileName ?? string.Empty,
            Culture: row.ProfileCulture ?? string.Empty,
            CreationDate: row.ProfileCreationDateUnixMs is { } creationDateUnixMs
                ? DateTimeOffset.FromUnixTimeMilliseconds(creationDateUnixMs)
                : DateTimeOffset.UnixEpoch);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
