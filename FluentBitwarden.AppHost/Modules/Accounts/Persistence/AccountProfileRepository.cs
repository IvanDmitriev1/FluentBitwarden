using Dapper;
using FluentBitwarden.AppHost.Modules.Accounts.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.Persistence.Mapping;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Modules.Accounts.Persistence;

internal sealed class AccountProfileRepository(SqliteTransaction transaction)
    : BaseRepository(transaction), IAccountProfileRepository
{
    public AccountProfile[] GetAccounts()
    {
        const string sql = """
                           SELECT
                               user_id,
                               email,
                               api_base,
                               identity_base,
                               notifications_base,
                               vault_base
                           FROM account_profiles
                           ORDER BY email ASC;
                           """;

        var rows = Connection.Query<AccountProfileMapper.AccountProfileRow>(
            sql,
            transaction: Transaction);

        return rows.Select(static row => AccountProfileMapper.ToDomain(row)).ToArray();
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
                               vault_base
                           FROM account_profiles
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        AccountProfileMapper.AccountProfileRow? row = Connection.QueryFirstOrDefault<AccountProfileMapper.AccountProfileRow>(
            sql,
            new
            {
                UserId = accountId.ToString()
            },
            transaction: Transaction);

        return row is null ? null : AccountProfileMapper.ToDomain(row);
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

        AccountProfileMapper.AccountProfileDetailsRow? row = Connection.QueryFirstOrDefault<AccountProfileMapper.AccountProfileDetailsRow>(
            sql,
            new
            {
                UserId = accountId.ToString()
            },
            transaction: Transaction);

        return row is null ? null : AccountProfileMapper.ToProfileDetails(row);
    }

    public void UpdateSyncedProfile(
        UserId accountId,
        VaultProfileResponse profile)
    {
        const string sql = """
                           UPDATE account_profiles
                           SET
                               email                         = @Email,
                               profile_name                  = @ProfileName,
                               profile_culture               = @ProfileCulture,
                               profile_creation_date_unix_ms = @ProfileCreationDateUnixMs,
                               profile_synced                = 1
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        var affectedRows = Connection.Execute(
            sql,
            AccountProfileMapper.ToUpdateSyncedParameters(accountId, profile),
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
                               vault_base
                           )
                           VALUES (
                               @UserId,
                               @Email,
                               @ApiBase,
                               @IdentityBase,
                               @NotificationsBase,
                               @VaultBase
                           )
                           ON CONFLICT(user_id) DO UPDATE SET
                               email                         = excluded.email,
                               api_base                      = excluded.api_base,
                               identity_base                 = excluded.identity_base,
                               notifications_base            = excluded.notifications_base,
                               vault_base                    = excluded.vault_base
                           """;

        Connection.Execute(
            sql,
            AccountProfileMapper.ToUpsertParameters(accountProfile),
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
}
