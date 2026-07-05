using MemoryPack;

namespace BitwardenApi.Identity.Contracts;

public enum IdentityTwoFactorProviderType
{
    Authenticator = 0,
    Email = 1,
    Duo = 2,
    YubiKey = 3,

    // Legacy / deprecated in favor of WebAuthn.
    U2f = 4,

    // Internal-ish provider used for "remember this device".
    Remember = 5,

    // Duo configured/enforced through an organization.
    OrganizationDuo = 6,

    //In login flow we do not support them

    // FIDO2/WebAuthn / passkey-style two-step login.
    //WebAuthn = 7,

    // Recovery-code flow.
    //RecoveryCode = 8
}

[MemoryPackable]
public readonly partial record struct IdentityTwoFactorProof(string Code, IdentityTwoFactorProviderType Provider);

[MemoryPackable]
public readonly partial record struct IdentityTwoFactorProviderOption(IdentityTwoFactorProviderType Provider);

[MemoryPackable]
public readonly partial record struct IdentityTwoFactorChallenge(IReadOnlyList<IdentityTwoFactorProviderOption> Providers);