using Microsoft.UI.Xaml;

namespace FluentBitwarden.Ui.Infrastructure.Windowing;

public interface IThemeChangeable
{
    void ApplyTheme(ElementTheme themeMode);
}
