using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;
using BitwardenApi.Shared.Cryptography;
using FluentBitwarden.Modules.Account.Models;

namespace FluentBitwarden.Modules.Account.Repositories;

internal readonly record struct AccountData(
    UserId UserId,
    string Email,
    string ApiBase,
    string IdentityBase,
    string NotificationsBase,
    EncryptedUserKey EncryptedUserKey,
    EncryptedPrivateKey EncryptedPrivateKey,
    KdfType KdfType,
    int KdfIterations,
    int? KdfMemoryMib,
    int? KdfParallelism,
    long LastSyncAtUnixMs,
    bool HasPin,
    bool HasWindowsHello);

internal static class AccountRepositoryMappingExtensions
{
    public static StoredAccount ToStoredAccount(this in AccountData row)
        => new(
            row.UserId,
            row.Email,
            new BitwardenEnvironment(
                new Uri(row.ApiBase),
                new Uri(row.IdentityBase),
                new Uri(row.NotificationsBase)),
            new AccountCryptoMaterial(
                ToKdfConfig(in row),
                row.EncryptedUserKey,
                row.EncryptedPrivateKey),
            DateTimeOffset.FromUnixTimeMilliseconds(row.LastSyncAtUnixMs),
            row.HasPin,
            row.HasWindowsHello);

    public static AccountData ToAccountData(this StoredAccount account)
    {
        KdfConfig kdfConfig = account.AccountCryptoMaterial.KdfConfig;

        return kdfConfig switch
        {
            KdfConfig.Pbkdf2 pbkdf2 => new AccountData(
                account.UserId,
                account.Email,
                account.Environment.ApiBase.ToString(),
                account.Environment.IdentityBase.ToString(),
                account.Environment.NotificationsBase.ToString(),
                account.AccountCryptoMaterial.EncryptedUserKey,
                account.AccountCryptoMaterial.EncryptedPrivateKey,
                KdfType.Pbkdf2Sha256,
                pbkdf2.Iterations,
                null,
                null,
                account.LastSyncAt.ToUnixTimeMilliseconds(),
                account.HasPin,
                account.HasWindowsHello),
            KdfConfig.Argon2Id argon2Id => new AccountData(
                account.UserId,
                account.Email,
                account.Environment.ApiBase.ToString(),
                account.Environment.IdentityBase.ToString(),
                account.Environment.NotificationsBase.ToString(),
                account.AccountCryptoMaterial.EncryptedUserKey,
                account.AccountCryptoMaterial.EncryptedPrivateKey,
                KdfType.Argon2Id,
                argon2Id.Iterations,
                argon2Id.MemoryMib,
                argon2Id.Parallelism,
                account.LastSyncAt.ToUnixTimeMilliseconds(),
                account.HasPin,
                account.HasWindowsHello),
            _ => throw new ArgumentOutOfRangeException(nameof(account))
        };
    }

    private static KdfConfig ToKdfConfig(in AccountData row)
        => row.KdfType switch
        {
            KdfType.Pbkdf2Sha256 => new KdfConfig.Pbkdf2(row.KdfIterations),
            KdfType.Argon2Id => new KdfConfig.Argon2Id(
                row.KdfIterations,
                row.KdfMemoryMib ?? throw new InvalidOperationException("Argon2Id account rows require kdf_memory_mib."),
                row.KdfParallelism ?? throw new InvalidOperationException("Argon2Id account rows require kdf_parallelism.")),
            _ => throw new ArgumentOutOfRangeException(nameof(row.KdfType), row.KdfType, "Unsupported KDF type.")
        };
}
