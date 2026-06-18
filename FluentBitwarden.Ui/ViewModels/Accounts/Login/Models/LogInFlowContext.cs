using FluentBitwarden.Contracts.Infrastructure;

namespace FluentBitwarden.ViewModels.Accounts.Login.Models;

internal sealed class LogInFlowContext
{
    public string Email { get; set; } = string.Empty;

    public BitwardenClientContext BitwardenContext { get; private set; } =
        new(BitwardenEnvironment.UnitedStates, DeviceIdentity.DeviceInfo);

    public void ChangeEnvironment(BitwardenEnvironment environment)
    {
        BitwardenContext = new BitwardenClientContext(environment, DeviceIdentity.DeviceInfo);
    }
}
