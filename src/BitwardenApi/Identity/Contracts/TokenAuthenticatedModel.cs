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
    ProtectedPrivateKey PrivateKey,
    MasterPasswordUnlockModel MasterPasswordUnlockModel);

public sealed record MasterPasswordUnlockModel(KdfConfig KdfConfig, string Salt, ProtectedUserKey UserKey);
