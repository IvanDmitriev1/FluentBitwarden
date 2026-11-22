using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Ui.Dialogs.Abstractions;
using FluentBitwarden.Ui.Infrastructure;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.UI;
using Navis.WinUI;
using Navis.WinUI.Abstractions;
using Windows.UI.ViewManagement;
using WinUIEx;

namespace FluentBitwarden.Ui.Windows;

internal sealed class WindowManager : IWindowManager, IContentDialogHost
{
    public WindowManager(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _uiSettings.ColorValuesChanged += OnUiSettingsOnColorValuesChanged;
    }

    private readonly ISettingsStore _settingsStore;
    private readonly UISettings _uiSettings = new();
    private WindowEx? _window;

    private WindowEx Window => _window ?? throw new InvalidOperationException("No window is open.");

    public WindowKind? CurrentWindowKind => _window switch
    {
        MainWindow => WindowKind.Main,
        OverlayWindow => WindowKind.Overlay,
        _ => null
    };

    public WindowId WindowId => Window.AppWindow.Id;

    public NativeWindowHandle NativeWindowHandle => new(Window.GetWindowHandle());

    public INavigation WindowNavigation
    {
        get
        {
            Frame rootFrame = Window switch
            {
                MainWindow mainWindow => mainWindow.RootFrame,
                OverlayWindow overlayWindow => overlayWindow.RootFrame,
                _ => throw new InvalidOperationException("Unknown window type.")
            };

            return NavigationHost.GetNavigation(rootFrame);
        }
    }

    public bool EnsureWindow(WindowKind kind)
    {
        if (_window is { } existing)
        {
            existing.ShowAndActivate();
            return false;
        }

        _window = kind switch
        {
            WindowKind.Main => new MainWindow(),
            WindowKind.Overlay => new OverlayWindow(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        ApplyTheme(_settingsStore.Get(UiSettingKeys.Appearance.ThemeKey));
        _window.ShowAndActivate();
        return true;
    }

    public async Task ShowDialogAsync(ContentDialog dialog, CancellationToken cancellationToken)
    {
        if (_window is not IContentDialogHost host)
            throw new InvalidOperationException("Unknown window type.");

        using var ctr = cancellationToken.Register(static state => HideDialog((ContentDialog)state!), dialog);
        await host.ShowDialogAsync(dialog, cancellationToken);

        static void HideDialog(ContentDialog dialog)
        {
            if (dialog.DispatcherQueue is null)
                return;

            _ = dialog.DispatcherQueue.TryEnqueue(dialog.Hide);
        }
    }

    public void ApplyTheme(ElementTheme theme)
    {
        if (_window is IThemeChangeable themed)
            themed.ApplyTheme(theme);
    }

    private void OnUiSettingsOnColorValuesChanged(UISettings sender, object args) =>
        _window?.DispatcherQueue.TryEnqueue(() => ApplyTheme(_settingsStore.Get(UiSettingKeys.Appearance.ThemeKey)));

}
