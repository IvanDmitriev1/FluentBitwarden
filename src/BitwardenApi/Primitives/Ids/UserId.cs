using MemoryPack;

namespace BitwardenApi.Primitives.Ids;

[MemoryPackable]
[StronglyTypedId(Template.Guid, "NullSafeSystemTextJsonConverter")]
public readonly partial struct UserId;
