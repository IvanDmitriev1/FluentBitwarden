using Windows.Storage;

namespace FluentBitwarden.Platform.Settings.Composition;

public static class ApplicationDataCompositeValueExtensions
{
    public static bool TryReadString(this ApplicationDataCompositeValue composite, string key, [NotNullWhen(true)] out string? value)
    {
        if (!composite.TryGetValue(key, out var storedValue))
        {
            value = null;
            return false;
        }

        if (storedValue is string text)
        {
            value = text;
            return true;
        }

        value = null;
        return false;
    }
}