using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Infrastructure.Window;

namespace FluentBitwarden.ViewModels.Settings.Models;

public sealed partial class WindowsHelloSettingValue(
    IAccountWindowsHelloIntegrationClient windowsHelloUnlockClient,
    IWindowManager windowManager) : ObservableObject
{
    private bool _isLoading = true;
    private bool _isApplying;

    [ObservableProperty]
    public partial bool IsSupported { get; private set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    public async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            WindowsHelloEnrollmentStatus status = await windowsHelloUnlockClient.GetEnrollmentAsync(
                new GetWindowsHelloEnrollmentRequest(),
                CancellationToken.None);
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
                WindowsHelloEnrollmentOutcome outcome = await windowsHelloUnlockClient.EnableAsync(
                    new EnableWindowsHelloEnrollmentRequest(
                        new NativeWindowHandle(windowManager.WindowHandle.ToInt64())));
                if (outcome != WindowsHelloEnrollmentOutcome.Enrolled)
                    IsEnabled = false;
            }
            else
            {
                await windowsHelloUnlockClient.DisableAsync(new DisableWindowsHelloEnrollmentRequest());
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
