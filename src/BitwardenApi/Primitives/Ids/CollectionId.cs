using MemoryPack;

namespace BitwardenApi.Primitives.Ids;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct CollectionId
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}
