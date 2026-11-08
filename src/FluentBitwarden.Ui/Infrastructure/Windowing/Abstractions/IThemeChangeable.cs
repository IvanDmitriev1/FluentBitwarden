using Microsoft.UI.Xaml;

namespace FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;

public interface IThemeChangeable
{
    void ApplyTheme(ElementTheme themeMode);
}
