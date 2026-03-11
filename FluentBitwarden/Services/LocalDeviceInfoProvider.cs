using BitwaredApi;
using BitwaredApi.Models.Auth;
using FluentBitwarden.Abstractions;

namespace FluentBitwarden.Services;

internal sealed class LocalDeviceInfoProvider(IAppSettingsStore appSettingsStore)
    : ILocalDeviceInfoProvider
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private BitwardenDeviceInfo? _deviceInfo;

    public string DeviceName { get; } = $"{Environment.MachineName} (FluentBitwarden)";

    public BitwardenDeviceInfo DeviceInfo
        => _deviceInfo ?? throw new InvalidOperationException("Local device info has not been initialized.");

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_deviceInfo is not null)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_deviceInfo is not null)
            {
                return;
            }

            string deviceIdentifier = await appSettingsStore
                .GetOrCreateDeviceIdentifierAsync(cancellationToken)
                .ConfigureAwait(false);

            _deviceInfo = new(
                DeviceType.WindowsDesktop,
                DeviceName,
                deviceIdentifier);
        }
        finally
        {
            _gate.Release();
        }
    }

    public BitwardenClientContext CreateClientContext(BitwardenEnvironment environment)
        => new(environment, DeviceInfo);
}
