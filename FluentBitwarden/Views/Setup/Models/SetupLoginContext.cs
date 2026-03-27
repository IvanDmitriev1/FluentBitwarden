using BitwardenApi.Context;

namespace FluentBitwarden.Views.Setup.Models;

internal sealed class SetupLoginContext(DeviceInfo deviceInfo, BitwardenEnvironment deviceInfoEnvironment)
{
    public DeviceInfo DeviceInfo { get; set; } = deviceInfo;
    public BitwardenEnvironment DeviceInfoEnvironment { get; set; } = deviceInfoEnvironment;
    public string Email { get; set; } = string.Empty;
}