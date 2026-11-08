using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;
using Microsoft.Extensions.Logging;
using WinUIEx;

namespace FluentBitwarden.Ui.Screens.Unlock;

public sealed partial class UnlockViewModel(
    AccountProfile profile,
    IAppSessionClient appSessionClient,
    IAccountWindowsHelloIntegrationClient accountWindowsHelloIntegrationClient,
    IWindowManager windowManager,
    ILogger<UnlockViewModel> logger) : ObservableObject
{

    [ObservableProperty]
    public partial AccountProfile SelectedAccount { get; set; } = profile;

    [ObservableProperty]
    public partial string? Error { get; private set; }

    [ObservableProperty]
    public partial bool IsUnlocking { get; private set; }

    [ObservableProperty]
    public partial int PasswordFocusRequest { get; private set; }

    [ObservableProperty]
    public partial bool IsWindowHelloButtonVisible { get; private set; }

    partial void OnSelectedAccountChanged(AccountProfile value)
    {
        RefreshWindowsHelloStatus(value).SafeFireAndForget();
    }

    [RelayCommand]
    private async Task Initialize()
    {
        await RefreshWindowsHelloStatus(SelectedAccount);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task Unlock(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            PasswordFocusRequest++;
            return Task.CompletedTask;
        }

        return OnUnlockCore(new SessionUnlockRequest.MasterPasswordRequest(SelectedAccount.UserId, password));

    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task UnlockWithWindowsHello()
    {
        return OnUnlockCore(new SessionUnlockRequest.WindowsHelloRequest(
            SelectedAccount.UserId,
            new NativeWindowHandle(windowManager.ActiveWindow.GetWindowHandle())));
    }

    private async Task OnUnlockCore(SessionUnlockRequest request)
    {
        IsUnlocking = true;
        SessionUnlockOutcome result;

        try
        {
            result = await appSessionClient.UnlockAsync(request);
        }
        finally
        {
            IsUnlocking = false;
        }

        if (result is SessionUnlockOutcome.Failure failure)
        {
            Error = failure.Reason;
            PasswordFocusRequest++;
            return;
        }
    }

    private async Task RefreshWindowsHelloStatus(AccountProfile account)
    {
        try
        {
            WindowsHelloEnrollmentStatus status = await accountWindowsHelloIntegrationClient.GetEnrollmentAsync(
                new GetWindowsHelloEnrollmentRequest(account.UserId));

            IsWindowHelloButtonVisible = status == WindowsHelloEnrollmentStatus.Enrolled;

        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception in OnSelectedAccountChanged");
        }
    }
}
