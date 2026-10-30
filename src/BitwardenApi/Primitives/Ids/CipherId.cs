using MemoryPack;

namespace BitwardenApi.Primitives.Ids;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String, "NullSafeSystemTextJsonConverter")]
public readonly partial struct CipherId
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}
