using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Views.Shell;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace FluentBitwarden.Services.Window;

internal sealed class WindowManager : IWindowManager
{
    private WindowEx? _window;

    public bool HasWindow => _window != null;

    public WindowEx ActiveWindow =>
        _window ?? throw new InvalidOperationException("No FluentBitwarden window is active.");

    public XamlRoot ActiveXamlRoot => ActiveWindow switch
    {
        MainWindow mainWindow => mainWindow.XamlRoot,
        OverlayWindow overlayWindow => overlayWindow.XamlRoot,
        { Content: FrameworkElement content } => content.XamlRoot,
        _ => throw new InvalidOperationException("The active FluentBitwarden window does not expose a XamlRoot.")
    };

    public void SetWindow(WindowEx window)
    {
        _window?.Close();
        _window = window;

        var currentTheme = SettingsStore.Instance.Get(UiSettingKeys.Appearance.ThemeKey);
        ApplyTheme(currentTheme);

        window.Closed += OnWindowClosed;
        window.ShowAndActivate();
    }

    public void CloseWindow()
    {
        var window = _window;
        _window = null;
        window?.Close();
    }

    public void ApplyTheme(ElementTheme themeMode)
    {
        var themeChangeable = ActiveWindow as IThemeChangeable;
        themeChangeable?.ApplyTheme(themeMode);
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (ReferenceEquals(_window, sender))
            _window = null;
    }
}
