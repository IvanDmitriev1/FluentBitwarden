using BitwaredApi;
using BitwaredApi.Models.Auth;
using FluentBitwarden.Abstractions;

namespace FluentBitwarden.Services;

internal sealed class LocalDeviceInfoProvider(IAppSettingsStore appSettingsStore)
    : ILocalDeviceInfoProvider
{
    public string DeviceName { get; } = $"{Environment.MachineName} (FluentBitwarden)";

    public BitwardenDeviceInfo DeviceInfo
    {
        get => _deviceInfo ?? throw new InvalidOperationException("Local device info has not been initialized.");
        set => _deviceInfo = value;
    }

    private BitwardenDeviceInfo? _deviceInfo;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_deviceInfo is not null)
            return;

        string deviceIdentifier = await appSettingsStore
            .GetOrCreateDeviceIdentifierAsync(cancellationToken)
            .ConfigureAwait(false);

        DeviceInfo = new BitwardenDeviceInfo(
            DeviceType.WindowsDesktop,
            DeviceName,
            deviceIdentifier);
    }

    public BitwardenClientContext CreateClientContext(BitwardenEnvironment environment) => new(environment, DeviceInfo);
}
