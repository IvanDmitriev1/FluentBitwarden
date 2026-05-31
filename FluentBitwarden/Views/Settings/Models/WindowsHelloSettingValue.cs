using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Extensions;

namespace FluentBitwarden.Views.Settings.Models;

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

        if (value)
        {
            _ = windowsHelloUnlockClient.EnableAsync(new EnableWindowsHelloRequest(windowManager.GetActiveWindowHandle()));
        }
        else
        {
            _ = windowsHelloUnlockClient.DisableAsync();
        }
    }
}
