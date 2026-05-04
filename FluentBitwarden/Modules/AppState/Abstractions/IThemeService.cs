using Microsoft.UI.Xaml;

namespace FluentBitwarden.Modules.AppState.Abstractions;

public interface IThemeService
{
    void Apply(ElementTheme themeMode);
}