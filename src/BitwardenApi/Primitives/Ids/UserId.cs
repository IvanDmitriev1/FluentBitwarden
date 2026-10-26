using MemoryPack;

namespace BitwardenApi.Primitives.Ids;

[MemoryPackable]
[StronglyTypedId(Template.Guid)]
public readonly partial struct UserId;
