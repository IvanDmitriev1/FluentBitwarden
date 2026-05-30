using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.Contracts.Accounts;

public interface IWindowsHelloUnlockClient
{
    ValueTask<WindowsHelloStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    ValueTask<WindowsHelloStatus> GetStatusAsync(GetWindowsHelloStatusRequest request, CancellationToken cancellationToken = default);

    ValueTask EnableAsync(EnableWindowsHelloRequest request, CancellationToken cancellationToken = default);
    ValueTask DisableAsync(CancellationToken cancellationToken = default);
}