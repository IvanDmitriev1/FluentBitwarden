using BitwardenApi.Common.MemoryPackFormatters;
using MemoryPack;

namespace BitwardenApi.Models;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct DeviceIdentifier;

[MemoryPackable(GenerateType.NoGenerate)]
[StronglyTypedId(Template.String)]
public readonly partial struct DeviceName;

[MemoryPackable]
public partial record DeviceInfo(
    [property: StronglyTypedIdFormatter<DeviceIdentifier>] DeviceIdentifier DeviceIdentifier,
    [property: StronglyTypedIdFormatter<DeviceName>] DeviceName DeviceName);
