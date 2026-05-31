using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock.Methods;
using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;

namespace FluentBitwarden.AppHost.Modules.Accounts;

internal class WindowsHelloUnlockClientHandler(
    WindowsHelloAccountUnlockMethod windowsHelloAccountUnlockMethod,
    IUnlockedAccountAccessor unlockedAccountAccessor) : IWindowsHelloUnlockClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.WindowsHello.GetCurrentAccountStatus)]
    public async ValueTask<WindowsHelloStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var userId = unlockedAccountAccessor.CurrentAccount.UserId;
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
            unlockedAccountAccessor.UserKey,
            request.OwnerWindowHandle);

        return ValueTask.CompletedTask;
    }

    [IpcMessageHandler(IpcMessageTypes.WindowsHello.Disable)]
    public ValueTask DisableAsync(CancellationToken cancellationToken = default)
    {
        var userId = unlockedAccountAccessor.CurrentAccount.UserId;
        windowsHelloAccountUnlockMethod.Disable(userId);

        return ValueTask.CompletedTask;
    }
}