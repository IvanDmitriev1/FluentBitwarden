namespace BitwardenApi.Identity.Models;


public sealed record RefreshedSession(
    SessionAccessToken SessionAccessToken,
    SessionRefreshToken SessionRefreshToken,
    TwoFactorToken? TwoFactorToken,
    DateTimeOffset ExpiresAt);

