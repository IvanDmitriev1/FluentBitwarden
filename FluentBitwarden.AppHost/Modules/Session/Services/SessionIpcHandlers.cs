using FluentBitwarden.Contracts;
using FluentBitwarden.Contracts.Ipc;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Session.Models;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.Modules.Session.Services;

internal static class SessionIpcHandlers
{
    public static IServiceCollection MapSessionIpcHandlers(this IServiceCollection services)
    {
        services.MapAccountManagerHandlers();
        services.MapWindowsHelloUnlockHandlers();


        return services;
    }

    private static void MapAccountManagerHandlers(this IServiceCollection services)
    {
        services.AddIpcRequestHandler<bool>(IpcMessageTypes.Account.HasActiveSession,
            static (IAccountSessionManager sessionManager) => sessionManager.ActiveSession is not null);

        services.AddIpcRequestHandler<AccountLoginRequest, AccountLoginOutcome>(
            static (AccountLoginRequest request, IAccountSessionManager sessionManager, CancellationToken cancellationToken) =>
                sessionManager.LogInAsync(request, cancellationToken));

        services.AddIpcRequestHandler<GetAccountsResponse>(IpcMessageTypes.Account.GetAccounts, static (IAccountSessionManager sessionManager) =>
        {
            var accounts = sessionManager.GetAccounts();
            return new GetAccountsResponse(accounts);
        });

        services.AddIpcRequestHandler<AccountUnlockRequest, AccountUnlockOutcome>(static (AccountUnlockRequest request,
            IAccountSessionManager sessionManager) => sessionManager.Unlock(request));
    }

    private static void MapWindowsHelloUnlockHandlers(this IServiceCollection services)
    {
        services.AddIpcRequestHandler<WindowsHelloStatus>(IpcMessageTypes.WindowsHello.GetCurrentAccountStatus,
            static async (WindowsHelloAccountUnlockMethod  windowsHelloAccountUnlockMethod, IAccountSessionManager accountSessionManager, CancellationToken ct) =>
            {
                var isSupported = await windowsHelloAccountUnlockMethod.IsSupportedAsync();
                var isEnabled = windowsHelloAccountUnlockMethod.IsEnabled(accountSessionManager.RequireActiveSession.Profile.UserId);

                return new WindowsHelloStatus(isSupported, isEnabled);
            });

        services.AddIpcRequestHandler<GetWindowsHelloStatusRequest, WindowsHelloStatus>(static async (GetWindowsHelloStatusRequest request, WindowsHelloAccountUnlockMethod windowsHelloAccountUnlockMethod) =>
        {
            var isEnabled = windowsHelloAccountUnlockMethod.IsEnabled(request.UserId);
            var isSupported = await windowsHelloAccountUnlockMethod.IsSupportedAsync();

            return new WindowsHelloStatus(isSupported, isEnabled);
        });

        services.AddIpcRequestHandler<EnableWindowsHelloRequest, IpcVoid>(static (EnableWindowsHelloRequest request, IAccountSessionManager accountSessionManager, WindowsHelloAccountUnlockMethod windowsHelloAccountUnlockMethod) =>
        {
            var session = accountSessionManager.RequireActiveSession;
            windowsHelloAccountUnlockMethod.Enable(session, request.OwnerWindowHandle);
            return new IpcVoid();
        });

        services.AddIpcRequestHandler<IpcVoid>(IpcMessageTypes.WindowsHello.Disable, static (IAccountSessionManager accountSessionManager, WindowsHelloAccountUnlockMethod windowsHelloAccountUnlockMethod) =>
        {
            var session = accountSessionManager.RequireActiveSession;
            windowsHelloAccountUnlockMethod.Disable(session.Profile.UserId);
            return new IpcVoid();
        });
    }
}