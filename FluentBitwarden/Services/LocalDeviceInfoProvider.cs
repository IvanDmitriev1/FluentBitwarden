using System.Text.Json;
using BitwaredApi;
using BitwaredApi.Models.Auth;
using FluentBitwarden.Core.Abstractions;

namespace FluentBitwarden.Services;

internal sealed class LocalDeviceInfoProvider(IAppPaths paths)
{
    private sealed record DeviceConfig(string DeviceIdentifier);

    public string DeviceName { get; } = $"{Environment.MachineName} (FluentBitwarden)";

    public async ValueTask<BitwardenDeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
        => new(
            DeviceType.WindowsDesktop,
            DeviceName,
            await _deviceIdentifier.Value.WaitAsync(cancellationToken).ConfigureAwait(false));

    public async ValueTask<BitwardenClientContext> GetClientContextAsync(
        BitwardenEnvironment environment,
        CancellationToken cancellationToken = default)
        => new(
            environment,
            await GetDeviceInfoAsync(cancellationToken).ConfigureAwait(false));

    private readonly Lazy<Task<string>> _deviceIdentifier = new(
        () => LoadOrCreateAsync(paths),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static async Task<string> LoadOrCreateAsync(IAppPaths paths)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(paths.ConfigFilePath)!);

        if (File.Exists(paths.ConfigFilePath))
        {
            await using var stream = File.OpenRead(paths.ConfigFilePath);
            DeviceConfig? config = await JsonSerializer
                .DeserializeAsync<DeviceConfig>(stream).ConfigureAwait(false);

            if (Guid.TryParse(config?.DeviceIdentifier, out Guid existing))
                return existing.ToString("D");
        }

        string newId = Guid.NewGuid().ToString("D");
        await using FileStream output = File.Create(paths.ConfigFilePath);
        await JsonSerializer.SerializeAsync(output, new DeviceConfig(newId)).ConfigureAwait(false);

        return newId;
    }
}
