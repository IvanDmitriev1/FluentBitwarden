using MemoryPack;

namespace BitwardenApi.Identity.Models;

[MemoryPackable]

public readonly partial record struct IdentityTwoFactorChallenge(IReadOnlyList<IdentityTwoFactorProviderOption> Providers);
