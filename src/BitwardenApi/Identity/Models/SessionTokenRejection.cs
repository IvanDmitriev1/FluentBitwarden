namespace BitwardenApi.Identity.Models;


public sealed record SessionTokenRejection(
    SessionTokenRejectionKind Kind,
    string Message,
    IdentityTwoFactorChallenge? TwoFactorChallenge = null);

