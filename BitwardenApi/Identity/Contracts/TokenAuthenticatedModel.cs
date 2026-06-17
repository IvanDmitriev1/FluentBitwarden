using BitwardenApi.Infrastructure.Cryptography;

namespace BitwardenApi.Identity.Contracts;

public sealed record TokenRefreshSessionModel(
    AccessToken AccessToken,
    RefreshToken RefreshToken,
    TwoFactorToken? TwoFactorToken,
    DateTimeOffset ExpiresAt);

public sealed record TokenAuthenticatedModel(
    AccessToken AccessToken,
    RefreshToken RefreshToken,
    TwoFactorToken? TwoFactorToken,
    DateTimeOffset ExpiresAt,
    EncryptedPrivateKey PrivateKey,
    MasterPasswordUnlockModel MasterPasswordUnlockModel);

public sealed record MasterPasswordUnlockModel(KdfConfig KdfConfig, string Salt, EncryptedUserKey UserKey);
