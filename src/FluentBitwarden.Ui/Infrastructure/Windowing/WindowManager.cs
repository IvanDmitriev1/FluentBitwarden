using FluentBitwarden.Platform.Settings;
using FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;
using WinUIEx;

namespace FluentBitwarden.Ui.Infrastructure.Windowing;

internal sealed class WindowManager : IWindowManager
{

    public WindowManager()
    {
        _uiSettings.ColorValuesChanged += OnUiSettingsOnColorValuesChanged;
    }

    private readonly UISettings _uiSettings = new();

    private WindowEx? _activeWindow;

    public WindowEx ActiveWindow =>
        _activeWindow ?? throw new InvalidOperationException("Application is not activated.");

    public void ReplaceWindow<TWindow>()
        where TWindow : WindowEx, new()
    {
        if (_activeWindow is not null)
        {
            _activeWindow.Closed -= OnWindowClosed;
            _activeWindow.Close();
        }

        _activeWindow = new TWindow();
        _activeWindow.Closed += OnWindowClosed;

        ApplyTheme(_activeWindow);
        _activeWindow.ShowAndActivate();
    }

    private void OnUiSettingsOnColorValuesChanged(UISettings sender, object args)
    {
        _activeWindow?.DispatcherQueue.TryEnqueue(() => ApplyTheme(_activeWindow));
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

    private static void ApplyTheme(WindowEx window)
    {
        if (window is not IThemeChangeable themeChangeable)
            return;

        var theme = SettingsStore.Instance.Get(UiSettingKeys.Appearance.ThemeKey);
        themeChangeable.ApplyTheme(theme);
    }
}
