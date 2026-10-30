using MemoryPack;

namespace BitwardenApi.Identity.Models;

[MemoryPackable]

public readonly partial record struct IdentityTwoFactorProviderOption(IdentityTwoFactorProviderType Provider);

