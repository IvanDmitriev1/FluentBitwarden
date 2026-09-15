using MemoryPack;

namespace BitwardenApi.Primitives.Ids;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct DeviceIdentifier;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct DeviceName;
