using FluentBitwarden.AppHost.Modules.Sessions.Abstractions;
using FluentBitwarden.AppHost.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;

namespace FluentBitwarden.AppHost.Modules.Accounts.Ipc;

internal sealed class WindowsHelloIpcHandler(
    WindowsHelloUnlocker windowsHelloUnlocker,
    IVaultSessionManager sessionManager) : IWindowsHelloUnlockClient, IIpcRequestsHandler
{
    [IpcMessageHandler(IpcMessageTypes.WindowsHello.GetCurrentAccountStatus)]
    public async ValueTask<WindowsHelloStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var session = sessionManager.GetUnlockedSession();
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

    public async ValueTask EnableAsync(
        EnableWindowsHelloRequest request,
        CancellationToken cancellationToken = default)
    {
        // Enable wraps the user key behind a Windows Hello prompt, so it borrows the key for as
        // long as the user takes to answer. Under the gate, a lock waits rather than disposing the
        // key mid-wrap; if the vault is already locked there is no key to wrap in the first place.
        bool enabled = await sessionManager.WithSessionAsync(
            (session, _) =>
            {
                windowsHelloUnlocker.Enable(session.UserKey, request.OwnerWindowHandle);
                return Task.FromResult(true);
            },
            lockedResult: false,
            cancellationToken);

        if (!enabled)
            throw new InvalidOperationException("The vault must be unlocked to enable Windows Hello.");
    }

    [IpcMessageHandler(IpcMessageTypes.WindowsHello.Disable)]
    public ValueTask DisableAsync(CancellationToken cancellationToken = default)
    {
        var session = sessionManager.GetUnlockedSession();
        windowsHelloUnlocker.Disable(session.Account.UserId);
        return ValueTask.CompletedTask;
    }
}
