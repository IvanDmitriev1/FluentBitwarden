using BitwardenApi.Infrastructure.Serialization;
using BitwardenApi.Primitives.Ids;
using MemoryPack;

namespace BitwardenApi.Primitives;

[MemoryPackable]
public partial record DeviceInfo(
    [property: StronglyTypedIdFormatter<DeviceIdentifier>] DeviceIdentifier DeviceIdentifier,
    [property: StronglyTypedIdFormatter<DeviceName>] DeviceName DeviceName);
