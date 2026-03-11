using BitwaredApi.Models.Auth;

namespace BitwaredApi;

public sealed record BitwardenDeviceInfo(
    DeviceType DeviceType,
    string DeviceName,
    string DeviceIdentifier);

public sealed record BitwardenClientContext(
    BitwardenEnvironment Environment,
    BitwardenDeviceInfo DeviceInfo);
