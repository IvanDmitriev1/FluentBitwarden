namespace BitwardenApi.Identity.Contracts;

public sealed record TokenRefreshSessionModel(
    SessionAccessToken SessionAccessToken,
    SessionRefreshToken SessionRefreshToken,
    TwoFactorToken? TwoFactorToken,
    DateTimeOffset ExpiresAt);

public sealed record TokenAuthenticatedModel(
    SessionAccessToken SessionAccessToken,
    SessionRefreshToken SessionRefreshToken,
    TwoFactorToken? TwoFactorToken,
    DateTimeOffset ExpiresAt,
    ProtectedPrivateKey PrivateKey,
    MasterPasswordUnlockModel MasterPasswordUnlockModel);

public sealed record MasterPasswordUnlockModel(KdfConfig KdfConfig, string Salt, ProtectedUserKey UserKey);
