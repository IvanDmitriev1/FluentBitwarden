using BitwardenApi.Modules.Identity.Models;
using Dapper;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Modules.Account.Repositories;

internal sealed class AccountRepository(SqliteTransaction transaction) : BaseRepository(transaction), IAccountRepository
{
    public IReadOnlyList<StoredAccount> GetAccounts()
    {
        CommandDefinition command = new(
            """
            SELECT
                user_id AS UserId,
                email AS Email,
                api_base AS ApiBase,
                identity_base AS IdentityBase,
                notifications_base AS NotificationsBase,
                encrypted_user_key AS EncryptedUserKey,
                encrypted_private_key AS EncryptedPrivateKey,
                kdf_type AS KdfType,
                kdf_iterations AS KdfIterations,
                kdf_memory_mib AS KdfMemoryMib,
                kdf_parallelism AS KdfParallelism,
                last_sync_at_unix_ms AS LastSyncAtUnixMs
            FROM accounts
            ORDER BY email COLLATE NOCASE;
            """, transaction: Transaction);

        var rows = Connection.Query<AccountData>(command);
        return rows.Select(static row => row.ToStoredAccount()).ToArray();
    }

    public StoredAccount? GetById(UserId accountId)
    {
        CommandDefinition command = new(
            """
            SELECT
                user_id AS UserId,
                email AS Email,
                api_base AS ApiBase,
                identity_base AS IdentityBase,
                notifications_base AS NotificationsBase,
                encrypted_user_key AS EncryptedUserKey,
                encrypted_private_key AS EncryptedPrivateKey,
                kdf_type AS KdfType,
                kdf_iterations AS KdfIterations,
                kdf_memory_mib AS KdfMemoryMib,
                kdf_parallelism AS KdfParallelism,
                last_sync_at_unix_ms AS LastSyncAtUnixMs
            FROM accounts
            WHERE user_id = @UserId COLLATE NOCASE;
            """,
            new { UserId = accountId }, transaction: Transaction);

        var rows = Connection.Query<AccountData>(command).Take(2).ToList();

        return rows.Count switch
        {
            0 => null,
            1 => rows[0].ToStoredAccount(),
            _ => throw new InvalidOperationException(
                $"Expected a single account row for user '{accountId}', but found {rows.Count}.")
        };
    }

    public void Upsert(StoredAccount account)
    {
        AccountData data = account.ToAccountData();

        CommandDefinition command = new(
            """
            INSERT INTO accounts (
                user_id,
                email,
                api_base,
                identity_base,
                notifications_base,
                encrypted_user_key,
                encrypted_private_key,
                kdf_type,
                kdf_iterations,
                kdf_memory_mib,
                kdf_parallelism,
                last_sync_at_unix_ms
            )
            VALUES (
                @UserId,
                @Email,
                @ApiBase,
                @IdentityBase,
                @NotificationsBase,
                @EncryptedUserKey,
                @EncryptedPrivateKey,
                @KdfType,
                @KdfIterations,
                @KdfMemoryMib,
                @KdfParallelism,
                @LastSyncAtUnixMs
            )
            ON CONFLICT(user_id) DO UPDATE SET
                email = excluded.email,
                api_base = excluded.api_base,
                identity_base = excluded.identity_base,
                notifications_base = excluded.notifications_base,
                encrypted_user_key = excluded.encrypted_user_key,
                encrypted_private_key = excluded.encrypted_private_key,
                kdf_type = excluded.kdf_type,
                kdf_iterations = excluded.kdf_iterations,
                kdf_memory_mib = excluded.kdf_memory_mib,
                kdf_parallelism = excluded.kdf_parallelism,
                last_sync_at_unix_ms = excluded.last_sync_at_unix_ms;
            """,
            data,
            transaction: Transaction);

        Connection.Execute(command);
    }

    public void Remove(UserId accountId)
    {
        CommandDefinition command = new(
            "DELETE FROM accounts WHERE user_id = @UserId COLLATE NOCASE;",
            new { UserId = accountId }, transaction: Transaction);

        Connection.Execute(command);
    }
}
