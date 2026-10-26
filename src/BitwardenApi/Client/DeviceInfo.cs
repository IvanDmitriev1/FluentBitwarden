using MemoryPack;

using BitwardenApi.Serialization;

namespace BitwardenApi.Client;

[MemoryPackable]
public partial record DeviceInfo(
    [property: StronglyTypedIdFormatter<DeviceIdentifier>] DeviceIdentifier DeviceIdentifier,
    [property: StronglyTypedIdFormatter<DeviceName>] DeviceName DeviceName);
