using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;

namespace BitwaredApi.Extensions;

internal static class JsonElementExtensions
{
    extension(JsonElement element)
    {
        public string GetRequiredStringProperty(string propertyName, string errorMessage)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.String)
            {
                throw new ServerVersionMismatchException(errorMessage);
            }

            return property.GetString()
                   ?? throw new ServerVersionMismatchException(errorMessage);
        }

        public string? GetOptionalStringProperty(string propertyName)
            => element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;

        public int GetRequiredInt32Property(string propertyName, string errorMessage)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Number)
            {
                throw new ServerVersionMismatchException(errorMessage);
            }

            return property.GetInt32();
        }

        public DateTimeOffset? GetOptionalDateTimeOffsetProperty(string propertyName,
            string? errorMessage = null)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property)
                || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (property.ValueKind != JsonValueKind.String)
            {
                throw new ServerVersionMismatchException(
                    errorMessage ?? $"Property '{propertyName}' was not a string.");
            }

            return DateTimeOffset.TryParse(property.GetString(), out DateTimeOffset parsed)
                ? parsed
                : null;
        }

        public bool TryGetFlexibleProperty(string propertyName, out JsonElement value)
        {
            if (element.TryGetProperty(propertyName, out value))
            {
                return true;
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                value = default;
                return false;
            }

            string lowerFirst = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
            if (!string.Equals(lowerFirst, propertyName, StringComparison.Ordinal)
                && element.TryGetProperty(lowerFirst, out value))
            {
                return true;
            }

            string lower = propertyName.ToLowerInvariant();
            if (!string.Equals(lower, propertyName, StringComparison.Ordinal)
                && !string.Equals(lower, lowerFirst, StringComparison.Ordinal)
                && element.TryGetProperty(lower, out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetAnyFlexibleProperty(out JsonElement value, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                if (element.TryGetFlexibleProperty(propertyName, out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
        }

        public JsonElement GetRequiredFlexibleProperty(string errorMessage,
            params string[] propertyNames)
            => element.TryGetAnyFlexibleProperty(out JsonElement value, propertyNames)
                ? value
                : throw new ServerVersionMismatchException(errorMessage);

        public string? GetOptionalFlexibleString(params string[] propertyNames)
            => element.TryGetAnyFlexibleProperty(out JsonElement value, propertyNames) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        public string GetRequiredFlexibleString(string errorMessage,
            params string[] propertyNames)
            => element.GetOptionalFlexibleString(propertyNames)
               ?? throw new ServerVersionMismatchException(errorMessage);
    }
}
