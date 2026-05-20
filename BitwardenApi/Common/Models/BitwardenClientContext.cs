namespace BitwardenApi.Models;

public readonly record struct BitwardenClientContext(
    BitwardenEnvironment Environment,
    DeviceInfo DeviceInfo);
