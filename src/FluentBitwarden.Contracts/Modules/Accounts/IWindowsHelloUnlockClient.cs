using FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;

namespace FluentBitwarden.Contracts.Modules.Accounts;

public interface IWindowsHelloUnlockClient
{
    ValueTask<WindowsHelloStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    ValueTask<WindowsHelloStatus> GetStatusAsync(GetWindowsHelloStatusRequest request, CancellationToken cancellationToken = default);

    ValueTask EnableAsync(EnableWindowsHelloRequest request, CancellationToken cancellationToken = default);
    ValueTask DisableAsync(CancellationToken cancellationToken = default);
}