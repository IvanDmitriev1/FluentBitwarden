using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.AppHost.Modules.Accounts.Persistence.Mapping;

internal static class AccountKeyMaterialMapper
{
    public sealed record AccountKeyMaterialRow(
        string UserId,
        string Salt,
        byte[] EncryptedUserKey,
        byte[] EncryptedPrivateKey,
        int KdfType,
        int KdfIterations,
        int? KdfMemoryMib,
        int? KdfParallelism);

    public static AccountKeyMaterial ToDomain(AccountKeyMaterialRow row) => new(
        UserId: UserId.Parse(row.UserId),
        Salt: row.Salt,
        KdfConfig: BuildKdf(row),
        ProtectedUserKey: ProtectedUserKey.Create(EncString.FromBytes(row.EncryptedUserKey)),
        ProtectedPrivateKey: ProtectedPrivateKey.Create(EncString.FromBytes(row.EncryptedPrivateKey)));

    public readonly record struct UpsertParameters(
        string UserId,
        string Salt,
        byte[] EncryptedUserKey,
        byte[] EncryptedPrivateKey,
        int KdfType,
        int KdfIterations,
        int? KdfMemoryMib,
        int? KdfParallelism);

    public static UpsertParameters ToUpsertParameters(AccountKeyMaterial keyMaterial)
    {
        var kdf = FlattenKdf(keyMaterial.KdfConfig);

        return new UpsertParameters(
            UserId: keyMaterial.UserId.ToString(),
            Salt: keyMaterial.Salt,
            EncryptedUserKey: keyMaterial.ProtectedUserKey.Value.ToByteArray(),
            EncryptedPrivateKey: keyMaterial.ProtectedPrivateKey.Value.ToByteArray(),
            KdfType: (int)kdf.Type,
            KdfIterations: kdf.Iterations,
            KdfMemoryMib: kdf.MemoryMib,
            KdfParallelism: kdf.Parallelism);
    }

    private static KdfConfig BuildKdf(AccountKeyMaterialRow row) =>
        (KdfType)row.KdfType switch
        {
            KdfType.Pbkdf2Sha256 => new KdfConfig.Pbkdf2(row.KdfIterations),

            KdfType.Argon2Id => new KdfConfig.Argon2Id(
                row.KdfIterations,
                row.KdfMemoryMib ??
                throw new InvalidOperationException("kdf_memory_mib is required for Argon2Id."),
                row.KdfParallelism ??
                throw new InvalidOperationException("kdf_parallelism is required for Argon2Id.")),

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
