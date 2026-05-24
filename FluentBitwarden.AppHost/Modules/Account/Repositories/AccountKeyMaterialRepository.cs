using BitwardenApi.Cryptography;
using BitwardenApi.Models;
using Dapper;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Account.Abstractions;
using FluentBitwarden.Modules.Account.Models;
using Microsoft.Data.Sqlite;

namespace FluentBitwarden.Modules.Account.Repositories;

internal sealed class AccountKeyMaterialRepository(SqliteTransaction transaction)
    : BaseRepository(transaction), IAccountKeyMaterialRepository
{
    internal sealed record AccountKeyMaterialRow(
        string UserId,
        string Salt,
        byte[] EncryptedUserKey,
        byte[] EncryptedPrivateKey,
        int KdfType,
        int KdfIterations,
        int? KdfMemoryMib,
        int? KdfParallelism);

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

        var row = Connection.QueryFirstOrDefault<AccountKeyMaterialRow>(
            sql,
            new
            {
                UserId = userId.ToString()
            },
            transaction: Transaction);

        return row is null ? null : MapToDomain(row);
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
                               salt = excluded.salt,
                               encrypted_user_key = excluded.encrypted_user_key,
                               encrypted_private_key = excluded.encrypted_private_key,
                               kdf_type = excluded.kdf_type,
                               kdf_iterations = excluded.kdf_iterations,
                               kdf_memory_mib = excluded.kdf_memory_mib,
                               kdf_parallelism = excluded.kdf_parallelism;
                           """;

        var kdf = FlattenKdf(keyMaterial.KdfConfig);

        Connection.Execute(
            sql,
            new
            {
                UserId = keyMaterial.UserId.ToString(),
                Salt = keyMaterial.Salt,
                EncryptedUserKey = keyMaterial.EncryptedUserKey.Value.ToByteArray(),
                EncryptedPrivateKey = keyMaterial.EncryptedPrivateKey.Value.ToByteArray(),
                KdfType = (int)kdf.Type,
                KdfIterations = kdf.Iterations,
                KdfMemoryMib = kdf.MemoryMib,
                KdfParallelism = kdf.Parallelism
            },
            transaction: Transaction);
    }

    private static AccountKeyMaterial MapToDomain(in AccountKeyMaterialRow row) => new(
        UserId: UserId.Parse(row.UserId),
        Salt: row.Salt,
        KdfConfig: BuildKdf(row),
        EncryptedUserKey: EncryptedUserKey.Create(EncString.FromBytes(row.EncryptedUserKey)),
        EncryptedPrivateKey: EncryptedPrivateKey.Create(EncString.FromBytes(row.EncryptedPrivateKey)));

    private static KdfConfig BuildKdf(in AccountKeyMaterialRow row) =>
        (KdfType)row.KdfType switch
        {
            KdfType.Pbkdf2Sha256 => new KdfConfig.Pbkdf2(row.KdfIterations),

            KdfType.Argon2Id => new KdfConfig.Argon2Id(
                row.KdfIterations,
                row.KdfMemoryMib ?? throw new InvalidOperationException("kdf_memory_mib is required for Argon2Id."),
                row.KdfParallelism ?? throw new InvalidOperationException("kdf_parallelism is required for Argon2Id.")),

            _ => throw new InvalidOperationException($"Unknown KdfType: {row.KdfType}.")
        };

    private static (KdfType Type, int Iterations, int? MemoryMib, int? Parallelism) FlattenKdf(KdfConfig kdf) =>
        kdf switch
        {
            KdfConfig.Pbkdf2(var iterations) => (
                KdfType.Pbkdf2Sha256,
                iterations,
                null,
                null),

            KdfConfig.Argon2Id(var iterations, var memoryMib, var parallelism) => (
                KdfType.Argon2Id,
                iterations,
                memoryMib,
                parallelism),

            _ => throw new InvalidOperationException($"Unknown KdfConfig type: {kdf.GetType().Name}.")
        };
}
