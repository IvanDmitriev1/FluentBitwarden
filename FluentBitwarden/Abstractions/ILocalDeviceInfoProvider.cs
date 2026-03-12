using BitwaredApi;

namespace FluentBitwarden.Abstractions;

public interface ILocalDeviceInfoProvider
{
    BitwardenDeviceInfo DeviceInfo { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    BitwardenClientContext CreateClientContext(BitwardenEnvironment environment);
}
