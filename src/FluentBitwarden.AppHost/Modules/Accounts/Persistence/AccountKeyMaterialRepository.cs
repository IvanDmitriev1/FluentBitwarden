using Dapper;
using FluentBitwarden.AppHost.Modules.Accounts.Persistence.Mapping;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.AppHost.Modules.Accounts.Persistence;

internal sealed class AccountKeyMaterialRepository(SqliteTransaction transaction) : BaseRepository(transaction)
{
    public AccountKeyMaterial? GetById(UserId userId)
    {
        const string sql = """
                           SELECT
                               user_id,
                               salt,
                               encrypted_user_key,
                               encrypted_private_key,
                               kdf_type,
                               kdf_iterations,
                               kdf_memory_mib,
                               kdf_parallelism
                           FROM account_key_material
                           WHERE user_id = @UserId;
                           """;

        var row = Connection.QueryFirstOrDefault<AccountKeyMaterialMapper.AccountKeyMaterialRow>(
            sql,
            new
            {
                UserId = userId.ToString()
            },
            transaction: Transaction);

        return row is null ? null : AccountKeyMaterialMapper.ToDomain(row);
    }

    public void Upsert(AccountKeyMaterial keyMaterial)
    {
        const string sql = """
                           INSERT INTO account_key_material (
                               user_id,
                               salt,
                               encrypted_user_key,
                               encrypted_private_key,
                               kdf_type,
                               kdf_iterations,
                               kdf_memory_mib,
                               kdf_parallelism
                           )
                           VALUES (
                               @UserId,
                               @Salt,
                               @EncryptedUserKey,
                               @EncryptedPrivateKey,
                               @KdfType,
                               @KdfIterations,
                               @KdfMemoryMib,
                               @KdfParallelism
                           )
                           ON CONFLICT(user_id) DO UPDATE SET
                               salt                  = excluded.salt,
                               encrypted_user_key    = excluded.encrypted_user_key,
                               encrypted_private_key = excluded.encrypted_private_key,
                               kdf_type              = excluded.kdf_type,
                               kdf_iterations        = excluded.kdf_iterations,
                               kdf_memory_mib        = excluded.kdf_memory_mib,
                               kdf_parallelism       = excluded.kdf_parallelism;
                           """;

        Connection.Execute(
            sql,
            AccountKeyMaterialMapper.ToUpsertParameters(keyMaterial),
            transaction: Transaction);
    }

    public void Remove(UserId userId)
    {
        Connection.Execute(
            """
            DELETE FROM account_key_material
            WHERE user_id = @UserId;
            """,
            new
            {
                UserId = userId.ToString()
            },
            Transaction);
    }
}
