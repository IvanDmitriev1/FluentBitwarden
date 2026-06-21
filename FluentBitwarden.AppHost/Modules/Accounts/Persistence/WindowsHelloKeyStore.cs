using Dapper;
using FluentBitwarden.AppHost.Infrastructure.Data.Abstractions;

namespace FluentBitwarden.AppHost.Modules.Accounts.Persistence;

internal sealed class WindowsHelloKeyStore(ISqliteConnectionFactory connectionFactory)
{
    public void Store(UserId userId, byte[] protectedUserKey)
    {
        using var connection = connectionFactory.OpenConnection();
        connection.Execute(
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
            });
    }

    public byte[]? Get(UserId userId)
    {
        using var connection = connectionFactory.OpenConnection();
        return connection.QuerySingleOrDefault<byte[]>(
            """
            SELECT protected_user_key
            FROM account_tpm_cng_unlock_keys
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new
            {
                UserId = userId.ToString()
            });
    }

    public bool Exists(UserId userId)
    {
        using var connection = connectionFactory.OpenConnection();
        return connection.ExecuteScalar<bool>(
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
            });
    }

    public void Remove(UserId userId)
    {
        using var connection = connectionFactory.OpenConnection();
        connection.Execute(
            """
            DELETE FROM account_tpm_cng_unlock_keys
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new
            {
                UserId = userId.ToString()
            });
    }
}
