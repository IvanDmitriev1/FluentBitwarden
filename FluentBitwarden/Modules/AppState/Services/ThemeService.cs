using FluentBitwarden.Modules.AppState.Abstractions;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Modules.AppState.Services;

public sealed class ThemeService(ISettingsService settingsService) : IThemeService
{
    public ElementTheme CurrentSetting { get; private set; } = ElementTheme.Default;

    private WeakReference<FrameworkElement>? _rootElement;

    public void Initialize(FrameworkElement rootElement)
    {
        _rootElement = new WeakReference<FrameworkElement>(rootElement);

        var settings = settingsService.Get();
        CurrentSetting = settings.ThemeMode;

        Set(CurrentSetting);
    }

    public void Set(ElementTheme themeMode)
    {
        if (_rootElement?.TryGetTarget(out var frameworkElement) is null || frameworkElement is null)
            return;

        frameworkElement.RequestedTheme = themeMode;
    }
}