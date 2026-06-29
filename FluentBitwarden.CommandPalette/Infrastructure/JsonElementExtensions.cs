using System.Text.Json;

namespace FluentBitwarden.CommandPalette.Infrastructure;

internal static class JsonElementExtensions
{
    public static string GetStringProperty(this JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement property)
            ? property.GetString() ?? string.Empty
            : string.Empty;
}
