using BitwardenApi.Identity;

namespace BitwardenApi;

public sealed record BitwardenClientContext(
    BitwardenEnvironment Environment,
    DeviceInfo DeviceInfo);
