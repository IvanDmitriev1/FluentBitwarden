using System.Text.Json;

namespace BitwaredApi.Models.Auth;

public enum TwoFactorProviderType
{
    Authenticator = 0,
    Email = 1,
    Duo = 2,
    Yubikey = 3,
    U2f = 4,
    Remember = 5,
    OrganizationDuo = 6,
    WebAuthn = 7,
    RecoveryCode = 8,
}

public sealed record TwoFactorProviderOption(
    TwoFactorProviderType Provider,
    IReadOnlyDictionary<string, JsonElement> Metadata);

public sealed record TwoFactorChallenge(
    IReadOnlyList<TwoFactorProviderOption> Providers,
    bool CanRemember,
    string? Email = null,
    string? SsoEmail2FaSessionToken = null);
