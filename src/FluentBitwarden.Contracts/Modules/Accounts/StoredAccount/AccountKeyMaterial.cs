using BitwardenApi.Infrastructure.Cryptography;

namespace FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

public sealed record AccountKeyMaterial(
    UserId UserId,
    string Salt,
    KdfConfig KdfConfig,
    ProtectedUserKey ProtectedUserKey,
    ProtectedPrivateKey ProtectedPrivateKey);
