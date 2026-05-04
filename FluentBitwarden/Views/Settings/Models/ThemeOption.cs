using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System.Linq;

namespace FluentBitwarden.Views.Settings.Models;

public readonly record struct ThemeOption(ElementTheme Value, string Title)
{
    public static readonly ThemeOption[] Options =
    [
        new ThemeOption(ElementTheme.Default, "System"),
        new ThemeOption(ElementTheme.Light, "Light"),
        new ThemeOption(ElementTheme.Dark, "Dark"),
    ];

    public static ThemeOption FromValue(ElementTheme value) => Options.First(p => p.Value == value);
};

public sealed class ThemeOptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is ElementTheme theme ? ThemeOption.FromValue(theme) : ThemeOption.Options[0];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is ThemeOption option
            ? option.Value
            : ElementTheme.Default;
    }
}