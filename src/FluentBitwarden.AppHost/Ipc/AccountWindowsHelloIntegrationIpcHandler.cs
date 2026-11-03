using System.Security.Cryptography;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Ipc;

internal sealed class AccountWindowsHelloIntegrationIpcHandler(
    IAccountWindowsHelloService accountWindowsHelloService,
    IAppSessionAccessor appSessionAccessor) : IAccountWindowsHelloIntegrationClient, IIpcRequestsHandler
{
    public async Task<WindowsHelloEnrollmentStatus> GetEnrollmentAsync(GetWindowsHelloEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        if (!await accountWindowsHelloService.IsSupportedAsync())
        {
            return WindowsHelloEnrollmentStatus.Unavailable;
        }

        return accountWindowsHelloService.IsEnabled(request.UserId)
            ? WindowsHelloEnrollmentStatus.Enrolled
            : WindowsHelloEnrollmentStatus.NotEnrolled;
    }

    public async Task<WindowsHelloEnrollmentOutcome> EnableAsync(EnableWindowsHelloEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = appSessionAccessor.RequireUnlockedState();

            if (!await accountWindowsHelloService.IsSupportedAsync())
                return WindowsHelloEnrollmentOutcome.Unavailable;

            if (accountWindowsHelloService.IsEnabled(state.Account.UserId))
                return WindowsHelloEnrollmentOutcome.Enrolled;

            accountWindowsHelloService.Enable(state.AccountKeySession.UserKey, request.OwnerWindow);
            return WindowsHelloEnrollmentOutcome.Enrolled;
        }
        catch (OperationCanceledException)
        {
            return WindowsHelloEnrollmentOutcome.Cancelled;
        }
        catch (CryptographicException e)
        {
            //TODO add logging
            Debug.WriteLine(e);
            return WindowsHelloEnrollmentOutcome.Failed;
        }
    }

    public async Task DisableAsync(DisableWindowsHelloEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var state = appSessionAccessor.RequireUnlockedState();

        if (!await accountWindowsHelloService.IsSupportedAsync())
            return;

        if (accountWindowsHelloService.IsEnabled(state.Account.UserId))
            accountWindowsHelloService.Disable(state.Account.UserId);
    }
}
