using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Data;

namespace FluentBitwarden.Resources.Converters;

internal sealed class StringToUriConverter : IValueConverter
{
    public const string DefaultScheme = "https";

    public static bool TryConvert(string? uriString, [NotNullWhen(true)] out Uri? uri)
    {
        if (string.IsNullOrEmpty(uriString))
        {
            uri = null;
            return false;
        }

        if (Uri.TryCreate(uriString, UriKind.Absolute, out var absoluteUri))
        {
            uri = absoluteUri;
            return true;
        }

        var normalizedUrl = $"{DefaultScheme}://{uriString}";
        if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var normalizedUri))
        {
            uri = normalizedUri;
            return true;
        }

        uri = null;
        return false;
    }

    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is not string uriString || TryConvert(uriString, out var uri))
            return null;

        return uri;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}