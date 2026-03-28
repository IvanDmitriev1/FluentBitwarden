namespace BitwardenApi.Shared.Context;

public readonly record struct BitwardenClientContext(
    BitwardenEnvironment Environment,
    DeviceInfo DeviceInfo);
