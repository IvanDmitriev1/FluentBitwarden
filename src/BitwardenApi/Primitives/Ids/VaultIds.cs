using MemoryPack;

namespace BitwardenApi.Primitives.Ids;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct CipherId
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct FolderId
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct CollectionId
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}
