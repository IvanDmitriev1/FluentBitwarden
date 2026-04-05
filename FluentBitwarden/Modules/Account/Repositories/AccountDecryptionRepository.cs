using System.Linq;
using BitwardenApi.Modules.Identity.Models;
using FluentBitwarden.Modules.Account.Abstractions;
using Dapper;
using FluentBitwarden.Data;
using Microsoft.Data.Sqlite;
using BitwardenApi.Cryptography;

namespace FluentBitwarden.Modules.Account.Repositories;

internal sealed class AccountDecryptionRepository(SqliteTransaction transaction) : BaseRepository(transaction), IAccountDecryptionRepository
{
    public readonly record struct AccountDecryptionRow(
        string UserId,
        string Salt,
        string EncryptedUserKey,
        string EncryptedPrivateKey,
        int KdfType,
        int KdfIterations,
        int? KdfMemoryMib,
        int? KdfParallelism);

    private static AccountDecryption MapToDomain(in AccountDecryptionRow row) => new(
        UserId: UserId.Parse(row.UserId), 
        Salt: row.Salt,
        KdfConfig: BuildKdf(row),
        EncryptedUserKey: EncryptedUserKey.Parse(row.EncryptedUserKey),
        EncryptedPrivateKey: EncryptedPrivateKey.Parse(row.EncryptedPrivateKey));

    private static KdfConfig BuildKdf(in AccountDecryptionRow row) =>
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
            KdfConfig.Pbkdf2(var iterations) => (KdfType.Pbkdf2Sha256, iterations, null, null),
            KdfConfig.Argon2Id(var iterations, var memory, var para) => (KdfType.Argon2Id, iterations, memory, para),
            _ => throw new InvalidOperationException($"Unknown KdfConfig type: {kdf.GetType().Name}.")
        };

    public AccountDecryption? GetById(UserId userId)
    {
        const string sql = """
                           SELECT *
                           FROM account_decryption
                           WHERE user_id = @UserId;
                           """;

        var row = Connection.QueryFirstOrDefault<AccountDecryptionRow>(sql,
            new
            {
                UserId = userId.ToString()
            }, transaction: Transaction);

        return row == default ? null : MapToDomain(row);
    }

    public void Upsert(AccountDecryption decryption)
    {
        const string sql = """
                           INSERT INTO account_decryption
                               (user_id, salt, encrypted_user_key, encrypted_private_key, kdf_type, kdf_iterations, kdf_memory_mib, kdf_parallelism)
                           VALUES
                               (@UserId, @Salt, @EncryptedUserKey, @EncryptedPrivateKey, @KdfType, @KdfIterations, @KdfMemoryMib, @KdfParallelism)
                           ON CONFLICT(user_id) DO UPDATE SET
                               salt                 = excluded.salt,
                               encrypted_user_key   = excluded.encrypted_user_key,
                               encrypted_private_key = excluded.encrypted_private_key,
                               kdf_type             = excluded.kdf_type,
                               kdf_iterations       = excluded.kdf_iterations,
                               kdf_memory_mib       = excluded.kdf_memory_mib,
                               kdf_parallelism      = excluded.kdf_parallelism;
                           """;

        var (kdfType, kdfIterations, kdfMemoryMib, kdfParallelism) = FlattenKdf(decryption.KdfConfig);

        Connection.Execute(sql, new
        {
            UserId = decryption.UserId.ToString(),
            Salt = decryption.Salt,
            EncryptedUserKey = decryption.EncryptedUserKey.ToString(),
            EncryptedPrivateKey = decryption.EncryptedPrivateKey.ToString(),
            KdfType = (int)kdfType,
            KdfIterations = kdfIterations,
            KdfMemoryMib = kdfMemoryMib,
            KdfParallelism = kdfParallelism,
        }, transaction: Transaction);
    }
}