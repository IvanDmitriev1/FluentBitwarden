using FluentBitwarden.Contracts.Modules.AppState;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Extensions;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Shell.Overlay;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace FluentBitwarden.Infrastructure.Implementations;

internal sealed class WindowManager : IWindowManager, IThemeService
{
    private ElementTheme _theme = SettingsStore.Instance.Get(UiSettingKeys.Appearance.ThemeKey);

    public Window? ActiveWindow { get; private set; }

    public void SetWindow(WindowEx window)
    {
        ActiveWindow?.Close();
        ActiveWindow = window;
        window.Closed += OnWindowClosed;
        window.ShowAndActivate();
    }

    public void CloseWindow()
    {
        var window = ActiveWindow;
        ActiveWindow = null;
        window?.Close();
    }

    public void Apply(ElementTheme themeMode)
    {
        _theme = themeMode;
        ApplyThemeToWindow(ActiveWindow);
    }

    private void ApplyThemeToWindow(Window? window)
    {
        switch (window)
        {
            case MainWindow mainWindow:
                mainWindow.ApplyTheme(_theme);
                break;

            case OverlayWindow overlayWindow:
                overlayWindow.ApplyTheme(_theme);
                break;
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (ReferenceEquals(ActiveWindow, sender))
            ActiveWindow = null;
    }
}
