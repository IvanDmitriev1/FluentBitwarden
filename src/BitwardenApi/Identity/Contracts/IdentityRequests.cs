namespace BitwardenApi.Identity.Contracts;

public sealed record PasswordAuthenticationRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPasswordHash,
    string Scope = "api offline_access");

public sealed record PasswordTwoFactorAuthenticationRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPasswordHash,
    IdentityTwoFactorProof TwoFactor,
    string Scope = "api offline_access");

public sealed record RefreshAuthenticationRequest(
    BitwardenClientContext Context,
    RefreshToken RefreshToken,
    string Scope = "api offline_access");

public sealed record DeviceAuthenticationRequest(
    BitwardenClientContext Context,
    string Email,
    string OneTimeAccessCode,
    AuthRequestId? AuthRequestId = null,
    string Scope = "api offline_access");

public sealed record AuthorizationCodeLoginRequest(
    BitwardenClientContext Context,
    string Code,
    string RedirectUri,
    string CodeVerifier,
    string Scope = "api offline_access");
