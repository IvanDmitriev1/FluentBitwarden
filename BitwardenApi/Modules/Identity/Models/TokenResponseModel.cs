using BitwardenApi.Shared.Cryptography;

namespace BitwardenApi.Modules.Identity.Models;

public sealed record TokenResponseModel(
    AccessToken AccessToken,
    RefreshToken RefreshToken,
    TwoFactorToken? TwoFactorToken,
    DateTimeOffset ExpiresAt,
    EncryptedPrivateKey PrivateKey,
    MasterPasswordUnlockModel MasterPasswordUnlockModel);

public sealed record MasterPasswordUnlockModel(KdfConfig KdfConfig, string Salt, EncryptedUserKey UserKey);