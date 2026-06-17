using Microsoft.UI.Xaml;

namespace FluentBitwarden.Infrastructure.Windowing;

public interface IThemeChangeable
{
    void ApplyTheme(ElementTheme themeMode);
}