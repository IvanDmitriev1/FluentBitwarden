using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;

namespace FluentBitwarden.Views.Settings.Models;

public sealed partial class WindowsHelloSettingValue(
    IAccountSessionManager accountSessionManager,
    WindowsHelloAccountUnlockMethod windowsHelloAccountUnlockMethod) : ObservableObject
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
            IsSupported = await windowsHelloAccountUnlockMethod.IsSupportedAsync();
            IsEnabled = IsSupported &&
                        accountSessionManager.ActiveSession is not null &&
                        windowsHelloAccountUnlockMethod.IsEnabled(
                            accountSessionManager.RequireActiveSession.Profile.UserId);
        }
        finally
        {
            _isLoading = false;
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_isLoading || accountSessionManager.ActiveSession is null)
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
