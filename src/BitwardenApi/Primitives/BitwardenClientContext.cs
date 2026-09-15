using MemoryPack;

namespace BitwardenApi.Primitives;

[MemoryPackable]
public readonly partial record struct BitwardenClientContext(
    BitwardenEnvironment Environment,
    DeviceInfo DeviceInfo);
