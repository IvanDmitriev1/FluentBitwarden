using BitwardenApi.Cryptography;

namespace FluentBitwarden.AppHost.Modules.Accounts.StoredAccounts.Models;

public sealed record AccountKeyMaterial(
    UserId UserId,
    string Salt,
    KdfConfig KdfConfig,
    EncryptedUserKey EncryptedUserKey,
    EncryptedPrivateKey EncryptedPrivateKey);
