using MemoryPack;

namespace BitwardenApi.Primitives.Ids;

[MemoryPackable]
[StronglyTypedId(Template.Guid, "NullSafeSystemTextJsonConverter")]
public readonly partial struct OrganizationId
{
    public bool IsEmpty => Value == Guid.Empty;
}
