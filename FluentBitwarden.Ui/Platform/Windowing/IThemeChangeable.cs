using Microsoft.UI.Xaml;

namespace FluentBitwarden.Platform.Windowing;

public interface IThemeChangeable
{
    void ApplyTheme(ElementTheme themeMode);
}