using BitwardenApi.Shared.Context;

namespace FluentBitwarden.Views.Setup.Models;

public sealed class SetupLoginContext(DeviceInfo deviceInfo, BitwardenEnvironment deviceInfoEnvironment)
{
    public DeviceInfo DeviceInfo { get; set; } = deviceInfo;
    public BitwardenEnvironment DeviceInfoEnvironment { get; set; } = deviceInfoEnvironment;
    public string Email { get; set; } = string.Empty;

    public BitwardenClientContext BitwardenClientContext =>
        new BitwardenClientContext(DeviceInfoEnvironment, DeviceInfo);
}