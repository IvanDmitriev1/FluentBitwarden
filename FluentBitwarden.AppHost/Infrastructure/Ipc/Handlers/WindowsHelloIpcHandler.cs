using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;

namespace FluentBitwarden.AppHost.Infrastructure.Ipc.Handlers;

internal sealed class WindowsHelloIpcHandler(
    WindowsHelloUnlocker windowsHelloUnlocker,
    IVaultSessionCoordinator vaultSessionCoordinator) : IWindowsHelloUnlockClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.WindowsHello.GetCurrentAccountStatus)]
    public async ValueTask<WindowsHelloStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var session = vaultSessionCoordinator.GetUnlockedSession();
        var isSupported = await windowsHelloUnlocker.IsSupportedAsync();
        var isEnabled = windowsHelloUnlocker.IsEnabled(session.Account.UserId);

        return new WindowsHelloStatus(isSupported, isEnabled);
    }

    public async ValueTask<WindowsHelloStatus> GetStatusAsync(
        GetWindowsHelloStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var isSupported = await windowsHelloUnlocker.IsSupportedAsync();
        var isEnabled = windowsHelloUnlocker.IsEnabled(request.UserId);

        return new WindowsHelloStatus(isSupported, isEnabled);
    }

    public ValueTask EnableAsync(
        EnableWindowsHelloRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = vaultSessionCoordinator.GetUnlockedSession();
        windowsHelloUnlocker.Enable(session.UserKey, request.OwnerWindowHandle);
        return ValueTask.CompletedTask;
    }

    [IpcMessageHandler(IpcMessageTypes.WindowsHello.Disable)]
    public ValueTask DisableAsync(CancellationToken cancellationToken = default)
    {
        var session = vaultSessionCoordinator.GetUnlockedSession();
        windowsHelloUnlocker.Disable(session.Account.UserId);
        return ValueTask.CompletedTask;
    }
}
