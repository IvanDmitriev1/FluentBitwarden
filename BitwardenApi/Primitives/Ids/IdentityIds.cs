using MemoryPack;

namespace BitwardenApi.Primitives.Ids;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct AccessToken;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct RefreshToken;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct TwoFactorToken;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct WebAuthnLoginAssertionOptionsToken;

[MemoryPackable]
[StronglyTypedId(Template.Guid)]
public readonly partial struct UserId;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct AuthRequestId;

[MemoryPackable]
[StronglyTypedId(Template.Guid)]
public readonly partial struct OrganizationId
{
    public bool IsEmpty => Value == Guid.Empty;
}
