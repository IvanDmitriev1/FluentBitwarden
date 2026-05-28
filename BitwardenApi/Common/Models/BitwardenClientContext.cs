using MemoryPack;

namespace BitwardenApi.Models;

[MemoryPackable]
public readonly partial record struct BitwardenClientContext(
    BitwardenEnvironment Environment,
    DeviceInfo DeviceInfo);
