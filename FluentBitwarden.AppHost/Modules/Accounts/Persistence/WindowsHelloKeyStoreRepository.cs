using Dapper;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Modules.Accounts.Persistence;

internal sealed class WindowsHelloKeyStoreRepository(SqliteTransaction transaction) : BaseRepository(transaction)
{
    public void Store(UserId userId, byte[] protectedUserKey)
    {
        Connection.Execute(
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
            },
            Transaction);
    }

    public byte[]? Get(UserId userId)
    {
        return Connection.QuerySingleOrDefault<byte[]>(
            """
            SELECT protected_user_key
            FROM account_tpm_cng_unlock_keys
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new
            {
                UserId = userId.ToString()
            },
            Transaction);
    }

    public bool Exists(UserId userId)
    {
        return Connection.ExecuteScalar<bool>(
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
            Transaction);
    }

    public void Remove(UserId userId)
    {
        Connection.Execute(
            """
            DELETE FROM account_tpm_cng_unlock_keys
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new
            {
                UserId = userId.ToString()
            },
            Transaction);
    }
}
