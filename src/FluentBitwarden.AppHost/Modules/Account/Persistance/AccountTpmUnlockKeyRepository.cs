using Dapper;

namespace FluentBitwarden.AppHost.Modules.Account.Persistance;

internal class AccountTpmUnlockKeyRepository(IDbSession dbSession)
{
    public void Store(UserId userId, byte[] protectedUserKey)
    {
        dbSession.Connection.Execute(
            """
            INSERT INTO account_tpm_cng_unlock_keys (user_id, protected_user_key)
            VALUES (@UserId, @ProtectedUserKey)
            ON CONFLICT(user_id) DO UPDATE SET
                protected_user_key = excluded.protected_user_key;
            """,
            new
            {
                UserId = userId.ToString(),
                ProtectedUserKey = protectedUserKey
            }, transaction: dbSession.RequiredTransaction);
    }

    public byte[]? Get(UserId userId)
    {
        return dbSession.Connection.QuerySingleOrDefault<byte[]>(
            """
            SELECT protected_user_key
            FROM account_tpm_cng_unlock_keys
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new
            {
                UserId = userId.ToString()
            },
            transaction: dbSession.RequiredTransaction);
    }

    public bool Exists(UserId userId)
    {
        return dbSession.Connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM account_tpm_cng_unlock_keys
                WHERE user_id = @UserId COLLATE NOCASE
            );
            """,
            new
            {
                UserId = userId.ToString()
            },
            transaction: dbSession.RequiredTransaction);
    }

    public void Remove(UserId userId)
    {
        dbSession.Connection.Execute(
            """
            DELETE FROM account_tpm_cng_unlock_keys
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new
            {
                UserId = userId.ToString()
            },
            transaction: dbSession.RequiredTransaction);
    }
}
