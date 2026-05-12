using BitwardenApi.Cryptography;
using BitwardenApi.Models;

namespace FluentBitwarden.Modules.Account.Models;

public sealed record AccountKeyMaterial(
    UserId UserId,
    string Salt,
    KdfConfig KdfConfig,
    EncryptedUserKey EncryptedUserKey,
    EncryptedPrivateKey EncryptedPrivateKey);