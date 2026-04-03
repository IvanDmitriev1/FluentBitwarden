using BitwardenApi.Shared.Cryptography;

namespace BitwardenApi.Modules.Identity.Models;

public sealed record AccountDecryption(
    UserId UserId,
    string Salt,
    KdfConfig KdfConfig,
    EncryptedUserKey EncryptedUserKey,
    EncryptedPrivateKey EncryptedPrivateKey);