using MemoryPack;

namespace BitwardenApi.Models;

public enum TwoFactorProviderType
{
    Authenticator = 0,
    Email = 1,
    Duo = 2,
    YubiKey = 3,
    U2f = 4
}

[MemoryPackable]
public readonly partial record struct TwoFactorProof(
    string Code,
    TwoFactorProviderType Provider);

public readonly record struct TwoFactorProviderOption(
    TwoFactorProviderType Provider,
    IReadOnlyDictionary<string, JsonElement> Metadata);

[MemoryPackable]
public readonly partial record struct TwoFactorChallenge(
    IReadOnlyList<TwoFactorProviderOption> Providers);