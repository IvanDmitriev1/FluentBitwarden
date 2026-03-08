using System.Text.Json;
using BitwaredApi.Abstractions;
using BitwaredApi.Models.Auth;
using FluentBitwarden.Core.Abstractions;

namespace FluentBitwarden.Security;

public sealed class LocalDeviceInfoProvider(IAppPaths paths) : IDeviceInfoProvider
{
    private sealed record DeviceConfig(string DeviceIdentifier);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _deviceIdentifier;

    public DeviceType DeviceType => DeviceType.WindowsDesktop;

    public string DeviceName => $"{Environment.MachineName} (FluentBitwarden)";

    public async ValueTask<string> GetDeviceIdentifierAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_deviceIdentifier))
        {
            return _deviceIdentifier;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!string.IsNullOrWhiteSpace(_deviceIdentifier))
            {
                return _deviceIdentifier;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(paths.ConfigFilePath)!);

            if (File.Exists(paths.ConfigFilePath))
            {
                await using FileStream stream = File.OpenRead(paths.ConfigFilePath);
                DeviceConfig? config = await JsonSerializer.DeserializeAsync<DeviceConfig>(
                    stream,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (Guid.TryParse(config?.DeviceIdentifier, out Guid existing))
                {
                    _deviceIdentifier = existing.ToString("D");
                    return _deviceIdentifier;
                }
            }

            _deviceIdentifier = Guid.NewGuid().ToString("D");

            await using FileStream output = File.Create(paths.ConfigFilePath);
            await JsonSerializer.SerializeAsync(output, new DeviceConfig(_deviceIdentifier), cancellationToken: cancellationToken).ConfigureAwait(false);
            return _deviceIdentifier;
        }
        finally
        {
            _gate.Release();
        }
    }
}
