using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Views.Settings.Models;
using Microsoft.UI.Xaml;
using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;
using FluentBitwarden.UI.Controls.Lifecycle;

namespace FluentBitwarden.Views.Settings;

public sealed partial class SettingsPageViewModel(
    IThemeService themeService,
    IAccountSessionManager accountSessionManager,
    WindowsHelloAccountUnlockMethod windowsHelloAccountUnlockMethod)
    : ObservableObject, IPageLifecycleAware
{
    public SettingValue<ElementTheme> Theme { get; } = AppSettingKeys.Appearance.ThemeKey.Create(themeService.Apply);

    [ObservableProperty]
    public partial bool IsWindowsHelloSupported { get; private set; }

    [ObservableProperty]
    public partial bool IsWindowsHelloEnabled { get; set; }

    private bool _isLoading = true;

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        IsWindowsHelloSupported = await windowsHelloAccountUnlockMethod.IsSupportedAsync();
        if (IsWindowsHelloSupported)
        {
            IsWindowsHelloEnabled =
                windowsHelloAccountUnlockMethod.IsEnabled(accountSessionManager.RequireActiveSession.Profile.UserId);
        }

        _isLoading = false;
    }

    public void OnUnloading() { }
    
    partial void OnIsWindowsHelloEnabledChanged(bool value)
    {
        if (_isLoading)
            return;

        var accountSession = accountSessionManager.RequireActiveSession;
        if (value)
        {
            windowsHelloAccountUnlockMethod.Enable(accountSession);
        }
        else
        {
            windowsHelloAccountUnlockMethod.Disable(accountSession.Profile.UserId);
        }
    }
}
