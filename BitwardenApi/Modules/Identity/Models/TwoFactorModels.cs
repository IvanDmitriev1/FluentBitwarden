namespace BitwardenApi.Modules.Identity.Models;

public enum TwoFactorProviderType
{
    Authenticator = 0,
    Email = 1,
    Duo = 2,
    YubiKey = 3,
    U2f = 4
}

public readonly record struct TwoFactorProof(
    string Code,
    TwoFactorProviderType Provider);

public readonly record struct TwoFactorProviderOption(
    TwoFactorProviderType Provider,
    IReadOnlyDictionary<string, JsonElement> Metadata);

public readonly record struct TwoFactorChallenge(
    IReadOnlyList<TwoFactorProviderOption> Providers);