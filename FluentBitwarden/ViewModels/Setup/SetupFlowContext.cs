using BitwaredApi;

namespace FluentBitwarden.ViewModels.Setup;

/// <summary>
/// Carries data that flows across multiple setup steps.
/// </summary>
public sealed class SetupFlowContext
{
    public SetupFlowContext(BitwardenDeviceInfo deviceInfo)
    {
        DeviceInfo = deviceInfo;
        ClientContext = new BitwardenClientContext(BitwardenEnvironment.UnitedStates, DeviceInfo);
    }

    public BitwardenDeviceInfo DeviceInfo { get; }
    public BitwardenClientContext ClientContext { get; private set; }
    public string Email { get; set; } = string.Empty;

    public void ChangeEnvironment(BitwardenEnvironment environment)
    {
        ClientContext = new BitwardenClientContext(environment, DeviceInfo);
    }
}
