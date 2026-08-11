using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Startup;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using WinUIEx;

namespace FluentBitwarden.Infrastructure.Window;

internal sealed class WindowManager : IWindowManager
{
    private WindowEx? _activeWindow;

    private WindowEx Window => _activeWindow ?? throw new InvalidOperationException("There is no active window.");

    private Frame ActiveFrame => _activeWindow switch
    {
        MainWindow mainWindow => mainWindow.NavigationFrame,
        OverlayWindow overlayWindow => overlayWindow.NavigationFrame,
        _ => throw new InvalidOperationException("The active window is not a supported window type.")
    };

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

    public void ReplacePage<TPage>(IPageNavigationParameter? parameter = null) where TPage : Page
    {
        Frame frame = ActiveFrame;
        if (frame.Content is TPage && parameter is null)
        {
            return;
        }

        var pageType = typeof(TPage);
        if (frame.CurrentSourcePageType == pageType)
        {
            if (parameter is not null && frame.Content is ILifeCycleAwarePage page)
            {
                page.Reload(parameter);
            }

            return;
        }

        bool navigated = frame.Navigate(pageType, parameter);
        Debug.Assert(navigated, $"Navigation to {pageType.Name} failed.");

        frame.BackStack.Clear();
        frame.ForwardStack.Clear();
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

        ReplacePage<LoadingPage>();
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
