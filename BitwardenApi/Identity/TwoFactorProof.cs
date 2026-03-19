namespace BitwardenApi.Identity;

public enum TwoFactorProviderType
{
    Authenticator = 0,
    Email = 1,
    Duo = 2,
    YubiKey = 3,
    U2f = 4,
    Remember = 5,
    OrganizationDuo = 6,
    Fido2WebAuthn = 7,
}

public sealed record TwoFactorProof(
    string Code,
    TwoFactorProviderType Provider,
    bool Remember = false);
