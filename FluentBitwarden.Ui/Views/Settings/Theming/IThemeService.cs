using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Settings.Theming;

public interface IThemeService
{
    void Apply(ElementTheme themeMode);
}