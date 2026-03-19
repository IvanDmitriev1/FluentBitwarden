namespace BitwardenApi.Identity;

public sealed record PasswordTwoFactorLoginRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPasswordHash,
    TwoFactorProof TwoFactor,
    string Scope = "api offline_access",
    string ClientId = "desktop");
