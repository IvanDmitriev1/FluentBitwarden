using Dapper;

namespace FluentBitwarden.AppHost.Modules.Account.Persistance;

internal sealed class AccountKeyMaterialRepository(IDbSession dbSession)
{
    public AccountKeyMaterial? GetById(UserId userId)
    {
        const string sql = """
                           SELECT
                               user_id AS UserId,
                               salt AS Salt,
                               encrypted_user_key AS EncryptedUserKey,
                               encrypted_private_key AS EncryptedPrivateKey,
                               kdf_type AS KdfType,
                               kdf_iterations AS KdfIterations,
                               kdf_memory_mib AS KdfMemoryMib,
                               kdf_parallelism AS KdfParallelism
                           FROM account_key_material
                           WHERE user_id = @UserId;
                           """;

        AccountKeyMaterialMapper.Row? row = dbSession.Connection.QuerySingleOrDefault<AccountKeyMaterialMapper.Row>(
            sql,
            AccountKeyMaterialMapper.ToUserIdParameters(userId),
            transaction: dbSession.Transaction);

        return AccountKeyMaterialMapper.ToDomain(row);
    }

    // Dapper.AOT currently emits invalid SQLite row values for byte[] parameters.
    [DapperAot(false)]
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

        dbSession.Connection.Execute(
            sql,
            AccountKeyMaterialMapper.ToSqlParameters(keyMaterial),
            transaction: dbSession.RequiredTransaction);
    }

    public void Remove(UserId userId)
    {
        const string sql = """
                           DELETE FROM account_key_material
                           WHERE user_id = @UserId;
                           """;

        dbSession.Connection.Execute(
            sql,
            AccountKeyMaterialMapper.ToUserIdParameters(userId),
            transaction: dbSession.RequiredTransaction);
    }
}
