using Dapper;

namespace FluentBitwarden.AppHost.Modules.Account.Persistance;

internal sealed class AccountBitwardenSessionTokenRepository(IDbSession dbSession)
{
    public void Store(UserId userId, RefreshToken token)
    {
        const string sql = """
                           INSERT INTO account_session_tokens (
                               user_id,
                               protected_refresh_token
                           )
                           VALUES (
                               @UserId,
                               @ProtectedRefreshToken
                           )
                           ON CONFLICT(user_id) DO UPDATE SET
                               protected_refresh_token = excluded.protected_refresh_token;
                           """;

        dbSession.Connection.Execute(
            sql,
            AccountBitwardenSessionTokenMapper.ToStoreParameters(userId, token),
            transaction: dbSession.RequiredTransaction);
    }

    public RefreshToken Get(UserId userId)
    {
        const string sql = """
                           SELECT protected_refresh_token AS ProtectedRefreshToken
                           FROM account_session_tokens
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        AccountBitwardenSessionTokenMapper.Row? row =
            dbSession.Connection.QuerySingleOrDefault<AccountBitwardenSessionTokenMapper.Row>(
                sql,
                AccountBitwardenSessionTokenMapper.ToUserIdParameters(userId),
                transaction: dbSession.RequiredTransaction);

        return AccountBitwardenSessionTokenMapper.ToDomain(row);
    }

    public void Remove(UserId userId)
    {
        const string sql = """
                           DELETE FROM account_session_tokens
                           WHERE user_id = @UserId COLLATE NOCASE;
                           """;

        dbSession.Connection.Execute(
            sql,
            AccountBitwardenSessionTokenMapper.ToUserIdParameters(userId),
            transaction: dbSession.RequiredTransaction);
    }
}
