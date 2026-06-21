using Microsoft.UI.Xaml;

namespace FluentBitwarden.Services.Window;

public interface IThemeChangeable
{
    void ApplyTheme(ElementTheme themeMode);
}