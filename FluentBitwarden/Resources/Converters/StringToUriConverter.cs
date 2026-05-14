using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Resources.Converters;

internal sealed class StringToUriConverter
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
}