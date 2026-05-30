using BitwardenApi.Models;
using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.Contracts.Accounts;

public interface IWindowsHelloUnlockClient
{
    ValueTask<WindowsHelloStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    ValueTask<WindowsHelloStatus> GetStatusAsync(UserId userId, CancellationToken cancellationToken = default);

    ValueTask EnableAsync(
        IntPtr ownerWindowHandle,
        CancellationToken cancellationToken = default);

    ValueTask DisableAsync(CancellationToken cancellationToken = default);
}