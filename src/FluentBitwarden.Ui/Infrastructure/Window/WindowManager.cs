using FluentBitwarden.Views.Shell;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace FluentBitwarden.Infrastructure.Window;

internal sealed class WindowManager : IWindowManager
{
    private WindowEx? _activeWindow;

    public WindowManager()
    {
    }
/*

    private WindowEx Window => _activeWindow ?? throw new InvalidOperationException("There is no active window.");

        _ => throw new InvalidOperationException("The active window is not a supported window type.")
    };

*/
    private WindowEx Window => _activeWindow!;

    public WindowMode ActiveMode => _activeWindow switch
    {
        MainWindow => WindowMode.Main,
        OverlayWindow => WindowMode.Overlay,
        _ => throw new InvalidOperationException("There is no active window.")
    };

    public IntPtr WindowHandle => (_activeWindow
        ?? throw new InvalidOperationException("There is no active window."))
        .GetWindowHandle();

    public XamlRoot XamlRoot => _activeWindow switch
    {
        MainWindow mainWindow => mainWindow.XamlRoot,
        OverlayWindow overlayWindow => overlayWindow.XamlRoot,
        { Content: FrameworkElement content } => content.XamlRoot,
        _ => throw new InvalidOperationException("The active window does not expose a XamlRoot.")
    };

    public void ShowOrCreateWindow(WindowMode mode)
    {
        if (_activeWindow is null || ActiveMode != mode)
        {
            ReplaceWindow(mode);
            return;
        }

        ActivateWindow();
    }

    public void ReplaceWindow(WindowMode mode)
    {
        switch (mode)
        {
            case WindowMode.Main:
                ReplaceWindow<MainWindow>();
                break;
            case WindowMode.Overlay:
                ReplaceWindow<OverlayWindow>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Unsupported window mode: {mode}.");
        }
    }

    public void ActivateWindow()
    {
        Window.ShowAndActivate();
    }

    public void MinimizeWindow()
    {
        Window.Minimize();
    }

    public void CloseWindow()
    {
        Window.Close();
    }


    public void ApplyTheme(ElementTheme themeMode)
    {
        var themeChangeable = _activeWindow as IThemeChangeable;
        themeChangeable?.ApplyTheme(themeMode);
    }

    private void ReplaceWindow<TWindow>()
        where TWindow : WindowEx, new()
    {
        if (_activeWindow is not null)
        {
            _activeWindow.Closed -= OnWindowClosed;
            _activeWindow.Close();
        }

        _activeWindow = new TWindow();
        _activeWindow.Closed += OnWindowClosed;

        var currentTheme = SettingsStore.Instance.Get(UiSettingKeys.Appearance.ThemeKey);
        ApplyTheme(currentTheme);

        _activeWindow.ShowAndActivate();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (!ReferenceEquals(_activeWindow, sender))
        {
            return;
        }

        _activeWindow.Closed -= OnWindowClosed;
        _activeWindow = null;
    }
}
