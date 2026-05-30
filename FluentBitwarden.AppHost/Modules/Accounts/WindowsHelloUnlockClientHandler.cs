using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Methods;
using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Accounts;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Session.Models;

namespace FluentBitwarden.AppHost.Modules.Accounts;

internal class WindowsHelloUnlockClientHandler(
    WindowsHelloAccountUnlockMethod windowsHelloAccountUnlockMethod,
    IUnlockedAccountAccessor accountAccessor,
    IUnlockedAccountKeyAccess accountKeyAccess) : IWindowsHelloUnlockClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.WindowsHello.GetCurrentAccountStatus)]
    public async ValueTask<WindowsHelloStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var userId = accountAccessor.CurrentAccount.UserId;
        var isSupported = await windowsHelloAccountUnlockMethod.IsSupportedAsync();
        var isEnabled = windowsHelloAccountUnlockMethod.IsEnabled(userId);

        return new WindowsHelloStatus(
            isSupported,
            isEnabled);
    }

    public async ValueTask<WindowsHelloStatus> GetStatusAsync(GetWindowsHelloStatusRequest request, CancellationToken cancellationToken = default)
    {
        var isSupported = await windowsHelloAccountUnlockMethod.IsSupportedAsync();
        var isEnabled = windowsHelloAccountUnlockMethod.IsEnabled(request.UserId);

        return new WindowsHelloStatus(
            isSupported,
            isEnabled);
    }

    public ValueTask EnableAsync(EnableWindowsHelloRequest request, CancellationToken cancellationToken = default)
    {
        windowsHelloAccountUnlockMethod.Enable(
            accountKeyAccess.UserKey,
            request.OwnerWindowHandle);

        return ValueTask.CompletedTask;
    }

    [IpcMessageHandler(IpcMessageTypes.WindowsHello.Disable)]
    public ValueTask DisableAsync(CancellationToken cancellationToken = default)
    {
        var userId = accountAccessor.CurrentAccount.UserId;
        windowsHelloAccountUnlockMethod.Disable(userId);

        return ValueTask.CompletedTask;
    }
}