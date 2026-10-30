using BitwardenApi.Client;
namespace BitwardenApi.Identity.Models;


public sealed record PasswordTwoFactorAuthenticationRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPasswordHash,
    IdentityTwoFactorProof TwoFactor,
    string Scope = "api offline_access");

