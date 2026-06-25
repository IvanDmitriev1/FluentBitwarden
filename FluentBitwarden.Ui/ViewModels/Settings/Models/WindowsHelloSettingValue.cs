using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;
using FluentBitwarden.Infrastructure.Window;
using WinUIEx;

namespace FluentBitwarden.ViewModels.Settings.Models;

public sealed partial class WindowsHelloSettingValue(
    IWindowsHelloUnlockClient windowsHelloUnlockClient,
    IWindowManager windowManager) : ObservableObject
{
    private bool _isLoading = true;

    [ObservableProperty]
    public partial bool IsSupported { get; private set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    public async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            var status = await windowsHelloUnlockClient.GetStatusAsync(CancellationToken.None);
            IsSupported = status.IsSupported;
            IsEnabled = IsSupported && status.IsEnabled;
        }
        finally
        {
            _isLoading = false;
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_isLoading)
            return;

        _ = value
            ? windowsHelloUnlockClient.EnableAsync(new EnableWindowsHelloRequest(windowManager.WindowHandle))
            : windowsHelloUnlockClient.DisableAsync();
    }
}
