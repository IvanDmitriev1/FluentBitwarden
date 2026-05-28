using MemoryPack;

namespace BitwardenApi.Models;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct CipherId;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct FolderId;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct CollectionId;
