using FluentBitwarden.Infrastructure.Navigation;
using FluentBitwarden.Views.Shell;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using FluentBitwarden.Views.Startup;
using WinUIEx;

namespace FluentBitwarden.Infrastructure.Window;

internal sealed class WindowManager : IWindowManager
{
    private WindowEx? _activeWindow;
    private bool HasWindow => _activeWindow is not null;

    private XamlRoot ActiveXamlRoot => _activeWindow switch
    {
        MainWindow mainWindow => mainWindow.XamlRoot,
        OverlayWindow overlayWindow => overlayWindow.XamlRoot,
        { Content: FrameworkElement content } => content.XamlRoot,
        _ => throw new InvalidOperationException("The active FluentBitwarden window does not expose a XamlRoot.")
    };

    private Frame ActiveFrame => _activeWindow switch
    {
        MainWindow mainWindow => mainWindow.NavigationFrame,
        OverlayWindow overlayWindow => overlayWindow.NavigationFrame,
        _ => throw new ArgumentOutOfRangeException(nameof(_activeWindow), "The active window is not a supported window type.")
    };


    public event EventHandler<IWindowManager, WindowMode>? WindowClosed;

    public WindowMode ActiveMode => _activeWindow switch
    {
        MainWindow => WindowMode.Main,
        OverlayWindow => WindowMode.Overlay,
        _ => throw new ArgumentOutOfRangeException(nameof(_activeWindow), "Cannot determine window mode for the active window.")
    };

    public IntPtr WindowHandle => (_activeWindow ?? throw new InvalidOperationException()).GetWindowHandle();

    public void ShowOrCreateWindow(WindowMode mode)
    {
        if (!HasWindow)
            ReplaceWindow(mode);
        else
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
        if (_activeWindow is null)
        {
            throw new InvalidOperationException("Cannot activate window because there is no active window.");
        }

        _activeWindow.ShowAndActivate();
    }

    public void CloseWindow()
    {
        if (_activeWindow is null)
        {
            throw new InvalidOperationException("Cannot close window because there is no active window.");
        }

        _activeWindow.Close();
    }

    public void ReplacePage<TPage>(IPageNavigationParameter? parameter = null) where TPage : Page
    {
        Frame frame = ActiveFrame;
        if (frame.Content is TPage && parameter is null)
            return;

        var pageType = typeof(TPage);
        if (frame.CurrentSourcePageType == pageType)
        {
            if (parameter is not null && frame.Content is ILifeCycleAwarePage page)
                page.Reload(parameter);

            return;
        }

        bool navigated = frame.Navigate(pageType, parameter);
        Debug.Assert(navigated, $"Navigation to {pageType.Name} failed.");

        frame.BackStack.Clear();
        frame.ForwardStack.Clear();
    }

    public async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog, CancellationToken cancellationToken = default)
    {
        dialog.XamlRoot = ActiveXamlRoot;
        return await dialog.ShowAsync().AsTask(cancellationToken);
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
        ArgumentNullException.ThrowIfNull(_activeWindow);

        WindowClosed?.Invoke(this, ActiveMode);

        _activeWindow.Closed -= OnWindowClosed;
        _activeWindow = null;
    }
}
