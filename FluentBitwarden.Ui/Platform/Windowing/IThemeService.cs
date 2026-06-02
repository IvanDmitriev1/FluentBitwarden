using Microsoft.UI.Xaml;

namespace FluentBitwarden.Platform.Windowing;

public interface IThemeService
{
    void Apply(ElementTheme themeMode);
}