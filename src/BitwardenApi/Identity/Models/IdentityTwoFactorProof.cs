using MemoryPack;

namespace BitwardenApi.Identity.Models;

[MemoryPackable]

public readonly partial record struct IdentityTwoFactorProof(string Code, IdentityTwoFactorProviderType Provider);

