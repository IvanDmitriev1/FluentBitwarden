using MemoryPack;

namespace BitwardenApi.Primitives.Ids;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct AttachmentId
{
    public bool IsEmpty => string.IsNullOrEmpty(Value);
}
