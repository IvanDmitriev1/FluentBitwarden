using MemoryPack;

namespace BitwardenApi.Client;

[MemoryPackable]
public readonly partial record struct BitwardenClientContext(
    BitwardenEnvironment Environment,
    DeviceInfo DeviceInfo);
