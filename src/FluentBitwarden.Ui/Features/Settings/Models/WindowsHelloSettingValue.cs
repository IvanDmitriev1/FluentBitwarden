using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Windows.Abstractions;

namespace FluentBitwarden.Ui.Features.Settings.Models;

public sealed partial class WindowsHelloSettingValue(
    IAppSessionClient appSessionClient,
    IAccountWindowsHelloIntegrationClient windowsHelloClient,
    IWindowManager windowManager) : ObservableObject
{
    private bool _isLoading = true;
    private bool _isApplying;

    [ObservableProperty]
    public partial bool IsSupported { get; private set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        _isLoading = true;
        try
        {
            AppSessionStateSnapshot state = await appSessionClient.GetStateAsync(new(), cancellationToken);
            AccountProfile? activeAccount = state switch
            {
                AppSessionStateSnapshot.Locked locked => locked.Account,
                AppSessionStateSnapshot.Unlocked unlocked => unlocked.Account,
                _ => null
            };

            if (activeAccount is null)
            {
                IsSupported = false;
                IsEnabled = false;
                return;
            }

            WindowsHelloEnrollmentStatus status = await windowsHelloClient.GetEnrollmentAsync(
                new GetWindowsHelloEnrollmentRequest(activeAccount.UserId), cancellationToken);
            IsSupported = status != WindowsHelloEnrollmentStatus.Unavailable;
            IsEnabled = status == WindowsHelloEnrollmentStatus.Enrolled;
        }
        finally
        {
            _isLoading = false;
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_isLoading || _isApplying)
            return;

        _ = ApplyEnabledAsync(value);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "A failed enrollment toggle must restore the previous visible setting state.")]
    private async Task ApplyEnabledAsync(bool enabled)
    {
        _isApplying = true;
        try
        {
            if (enabled)
            {
                WindowsHelloEnrollmentOutcome outcome = await windowsHelloClient.EnableAsync(
                    new EnableWindowsHelloEnrollmentRequest(windowManager.NativeWindowHandle));
                if (outcome != WindowsHelloEnrollmentOutcome.Enrolled)
                    IsEnabled = false;
            }
            else
            {
                await windowsHelloClient.DisableAsync(new DisableWindowsHelloEnrollmentRequest());
            }
        }
        catch (Exception)
        {
            IsEnabled = !enabled;
        }
        finally
        {
            _isApplying = false;
        }
    }
}
