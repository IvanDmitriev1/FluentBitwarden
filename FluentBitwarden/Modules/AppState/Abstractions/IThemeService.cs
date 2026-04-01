using Microsoft.UI.Xaml;

namespace FluentBitwarden.Modules.AppState.Abstractions;

public interface IThemeService
{
    ElementTheme CurrentSetting { get; }

    void Set(ElementTheme themeMode);
}