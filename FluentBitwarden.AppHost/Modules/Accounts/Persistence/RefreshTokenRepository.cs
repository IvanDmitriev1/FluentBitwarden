using System.Security.Cryptography;
using System.Text;
using Dapper;
using FluentBitwarden.AppHost.Data.Abstractions;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Modules.Accounts.Persistence;

internal sealed class RefreshTokenRepository(SqliteTransaction transaction) : BaseRepository(transaction)
{
    private static byte[] Entropy => "fbw_session_v1"u8.ToArray();

    public void Store(UserId userId, RefreshToken token)
    {
        byte[] plaintext = Encoding.UTF8.GetBytes(token.ToString());

        try
        {
            byte[] protectedBytes = ProtectedData.Protect(
                userData: plaintext,
                optionalEntropy: Entropy,
                scope: DataProtectionScope.CurrentUser);

            Connection.Execute(
                """
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
                """,
                new
                {
                    UserId = userId.ToString(),
                    ProtectedRefreshToken = protectedBytes
                },
                Transaction);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public RefreshToken Get(UserId userId)
    {
        byte[]? protectedBytes = Connection.QuerySingleOrDefault<byte[]>(
            """
            SELECT protected_refresh_token
            FROM account_session_tokens
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new
            {
                UserId = userId.ToString()
            },
            Transaction);

        if (protectedBytes is null)
            return RefreshToken.Empty;

        byte[] plaintext = [];

        try
        {
            plaintext = ProtectedData.Unprotect(
                encryptedData: protectedBytes,
                optionalEntropy: Entropy,
                scope: DataProtectionScope.CurrentUser);

            string tokenValue = Encoding.UTF8.GetString(plaintext);
            if (string.IsNullOrWhiteSpace(tokenValue))
                return RefreshToken.Empty;

            return RefreshToken.Parse(tokenValue);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public void Remove(UserId userId)
    {
        Connection.Execute(
            """
            DELETE FROM account_session_tokens
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new
            {
                UserId = userId.ToString()
            },
            Transaction);
    }
}
