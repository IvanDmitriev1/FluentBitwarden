using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Cryptography;

namespace FluentBitwarden.Modules.Account.Models;

public sealed record AccountCryptoMaterial(
    KdfConfig KdfConfig,
    EncryptedUserKey EncryptedUserKey,
    EncryptedPrivateKey EncryptedPrivateKey);