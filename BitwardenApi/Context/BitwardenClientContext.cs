namespace BitwardenApi.Context;

public sealed record BitwardenClientContext(
    BitwardenEnvironment Environment,
    DeviceInfo DeviceInfo);
