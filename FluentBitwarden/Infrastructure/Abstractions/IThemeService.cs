using Microsoft.UI.Xaml;

namespace FluentBitwarden.Infrastructure.Abstractions;

public interface IThemeService
{
    void Apply(ElementTheme themeMode);
}