using BitwaredApi.Models.Auth;

namespace BitwaredApi.Abstractions;

public interface IDeviceInfoProvider
{
    DeviceType DeviceType { get; }

    string DeviceName { get; }

    ValueTask<string> GetDeviceIdentifierAsync(CancellationToken cancellationToken = default);
}
