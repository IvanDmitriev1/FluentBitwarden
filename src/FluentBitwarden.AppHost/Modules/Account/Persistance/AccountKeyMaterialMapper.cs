using BitwardenApi.Identity.Contracts;
using BitwardenApi.Infrastructure.Cryptography;
using BitwardenApi.Infrastructure.Cryptography.Enc;

namespace FluentBitwarden.AppHost.Modules.Account.Persistance;

internal static class AccountKeyMaterialMapper
{
    public static AccountKeyMaterial? ToDomain(Row? row)
    {
        if (row is null)
            return null;

        KdfConfig kdfConfig = row.KdfType switch
        {
            (int)KdfType.Pbkdf2Sha256 => new KdfConfig.Pbkdf2(row.KdfIterations),
            (int)KdfType.Argon2Id when row.KdfMemoryMib is { } memoryMib && row.KdfParallelism is { } parallelism =>
                new KdfConfig.Argon2Id(row.KdfIterations, memoryMib, parallelism),
            _ => throw new InvalidOperationException($"Unsupported KDF type '{row.KdfType}'.")
        };

        return new AccountKeyMaterial(
            UserId.Parse(row.UserId),
            row.Salt,
            kdfConfig,
            ProtectedUserKey.Create(EncString.FromBytes(row.EncryptedUserKey)),
            ProtectedPrivateKey.Create(EncString.FromBytes(row.EncryptedPrivateKey)));
    }

    public static Parameters ToSqlParameters(AccountKeyMaterial keyMaterial) =>
        keyMaterial.KdfConfig switch
        {
            KdfConfig.Pbkdf2 pbkdf2 => new Parameters(
                keyMaterial.UserId.ToString(),
                keyMaterial.Salt,
                keyMaterial.ProtectedUserKey.Value.ToByteArray(),
                keyMaterial.ProtectedPrivateKey.Value.ToByteArray(),
                (int)KdfType.Pbkdf2Sha256,
                pbkdf2.Iterations,
                null,
                null),
            KdfConfig.Argon2Id argon2Id => new Parameters(
                keyMaterial.UserId.ToString(),
                keyMaterial.Salt,
                keyMaterial.ProtectedUserKey.Value.ToByteArray(),
                keyMaterial.ProtectedPrivateKey.Value.ToByteArray(),
                (int)KdfType.Argon2Id,
                argon2Id.Iterations,
                argon2Id.MemoryMib,
                argon2Id.Parallelism),
            _ => throw new InvalidOperationException("Unsupported KDF configuration.")
        };

    public static UserIdParameters ToUserIdParameters(UserId userId) => new(userId.ToString());

    internal sealed class Row
    {
        public string UserId { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public byte[] EncryptedUserKey { get; set; } = [];
        public byte[] EncryptedPrivateKey { get; set; } = [];
        public int KdfType { get; set; }
        public int KdfIterations { get; set; }
        public int? KdfMemoryMib { get; set; }
        public int? KdfParallelism { get; set; }
    }

    internal sealed record Parameters(
        string UserId,
        string Salt,
        byte[] EncryptedUserKey,
        byte[] EncryptedPrivateKey,
        int KdfType,
        int KdfIterations,
        int? KdfMemoryMib,
        int? KdfParallelism);

    internal sealed record UserIdParameters(string UserId);
}
