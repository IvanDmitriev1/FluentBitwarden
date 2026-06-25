using Microsoft.UI.Xaml;

namespace FluentBitwarden.Infrastructure.Window;

public interface IThemeChangeable
{
    void ApplyTheme(ElementTheme themeMode);
}