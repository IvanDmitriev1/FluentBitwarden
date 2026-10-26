using BitwardenApi.Cryptography.Keys;
namespace BitwardenApi.Identity.Models;


public sealed record AuthenticatedSession(
    SessionAccessToken SessionAccessToken,
    SessionRefreshToken SessionRefreshToken,
    TwoFactorToken? TwoFactorToken,
    DateTimeOffset ExpiresAt,
    ProtectedPrivateKey PrivateKey,
    MasterPasswordUnlockData MasterPasswordUnlockModel);

