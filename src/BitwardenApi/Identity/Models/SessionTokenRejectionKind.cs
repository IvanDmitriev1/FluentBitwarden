namespace BitwardenApi.Identity.Models;


public enum SessionTokenRejectionKind
{
    InvalidCredentials,
    TwoFactorRequired,
    DeviceVerificationRequired
}
