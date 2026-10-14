using Dapper;
using FluentBitwarden.AppHost.Modules.Account.Persistance.Mappers;

namespace FluentBitwarden.AppHost.Modules.Account.Persistance;

internal sealed class AccountProfileRepository(IDbSession dbSession)
{
    public AccountProfile[] GetAccounts()
    {
        const string sql = """
                           SELECT
                               user_id AS UserId,
                               email AS Email,
                               api_base AS ApiBase,
                               identity_base AS IdentityBase,
                               notifications_base AS NotificationsBase,
                               vault_base AS VaultBase
                           FROM account_profiles
                           ORDER BY email ASC;
                           """;

        return dbSession.Connection.Query<AccountProfileMapper.Row>(sql, transaction: dbSession.Transaction)
            .Select(AccountProfileMapper.ToDomain).ToArray();
    }

    public AccountProfile? GetById(UserId accountId)
    {
        const string sql = """
                           SELECT
                               user_id AS UserId,
                               email AS Email,
                               api_base AS ApiBase,
                               identity_base AS IdentityBase,
                               notifications_base AS NotificationsBase,
                               vault_base AS VaultBase
                           FROM account_profiles
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        AccountProfileMapper.Row? row = dbSession.Connection.QuerySingleOrDefault<AccountProfileMapper.Row>(
            sql,
            AccountProfileMapper.ToUserIdParameters(accountId),
            transaction: dbSession.Transaction);

        return AccountProfileMapper.ToDomainOrNull(row);
    }

    public AccountProfileDetails? GetProfileDetails(UserId accountId)
    {
        const string sql = """
                           SELECT
                               profile_name AS ProfileName,
                               profile_culture AS ProfileCulture,
                               profile_creation_date_unix_ms AS ProfileCreationDateUnixMs,
                               profile_synced AS ProfileSynced
                           FROM account_profiles
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        AccountProfileMapper.DetailsRow? row =
            dbSession.Connection.QuerySingleOrDefault<AccountProfileMapper.DetailsRow>(
                sql,
                AccountProfileMapper.ToUserIdParameters(accountId),
                transaction: dbSession.Transaction);

        return AccountProfileMapper.ToDetails(row);
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

        dbSession.Connection.Execute(
            sql,
            AccountProfileMapper.ToUpsertParameters(accountProfile),
            transaction: dbSession.RequiredTransaction);
    }

    public void Remove(UserId accountId)
    {
        const string sql = """
                           DELETE FROM account_profiles
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        dbSession.Connection.Execute(
            sql,
            AccountProfileMapper.ToUserIdParameters(accountId),
            transaction: dbSession.RequiredTransaction);
    }
}
