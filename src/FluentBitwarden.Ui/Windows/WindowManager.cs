using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Ui.Infrastructure;
using FluentBitwarden.Ui.Windows.Abstractions;
using Navis.WinUI;
using Navis.WinUI.Abstractions;
using Windows.UI.ViewManagement;
using WinUIEx;

namespace FluentBitwarden.Ui.Windows;

internal sealed class WindowManager : IWindowManager
{
    public WindowManager(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _uiSettings.ColorValuesChanged += OnUiSettingsOnColorValuesChanged;
    }

    private readonly ISettingsStore _settingsStore;
    private readonly UISettings _uiSettings = new();
    private WindowEx? _currentWindow;

    public WindowKind? ActiveWindow => _currentWindow switch
    {
        MainWindow => WindowKind.Main,
        OverlayWindow => WindowKind.Overlay,
        null => null,
        _ => throw new InvalidOperationException("Unknown window type.")
    };

    public NativeWindowHandle NativeWindowHandle => new(
        (_currentWindow ?? throw new InvalidOperationException("Application is not activated.")).GetWindowHandle());

    public INavigation WindowNavigation
    {
        get
        {
            if (_currentWindow is null)
                throw new InvalidOperationException("Application is not activated.");

            Frame rootFrame = _currentWindow switch
            {
                MainWindow mainWindow => mainWindow.RootFrame,
                OverlayWindow overlayWindow => overlayWindow.RootFrame,
                _ => throw new InvalidOperationException("Unknown window type.")
            };

            return NavigationHost.GetNavigation(rootFrame);
        }
    }

    public bool EnsureWindow(WindowKind kind) => kind switch
    {
        WindowKind.Main => EnsureWindow<MainWindow>(),
        WindowKind.Overlay => EnsureWindow<OverlayWindow>(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public void CloseActiveWindow() => _currentWindow?.Close();

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (sender is not WindowEx window)
            return;

        window.Closed -= OnWindowClosed;
        if (ReferenceEquals(_currentWindow, window))
            _currentWindow = null;
    }

    public void ApplyTheme(ElementTheme theme)
    {
        if (_currentWindow is IThemeChangeable themeChangeable)
            themeChangeable.ApplyTheme(theme);
    }

    private void OnUiSettingsOnColorValuesChanged(UISettings sender, object args) =>
        _currentWindow?.DispatcherQueue.TryEnqueue(() => ApplyTheme(_currentWindow));

    private bool EnsureWindow<TWindow>() where TWindow : WindowEx, new()
    {
        if (_currentWindow is TWindow)
        {
            _currentWindow.ShowAndActivate();
            return false;
        }

        CloseActiveWindow();

        TWindow newWindow = new();
        _currentWindow = newWindow;
        newWindow.Closed += OnWindowClosed;

        ApplyTheme(newWindow);
        newWindow.ShowAndActivate();
        return true;
    }

    private void ApplyTheme(WindowEx? window)
    {
        if (window is not IThemeChangeable themeChangeable)
            return;

        themeChangeable.ApplyTheme(_settingsStore.Get(UiSettingKeys.Appearance.ThemeKey));
    }
}
