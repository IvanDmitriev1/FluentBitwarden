using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;

namespace BitwaredApi.Extensions;

internal static class Utf8JsonReaderExtensions
{
    public static void SkipValue(this ref Utf8JsonReader reader)
    {
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            reader.Skip();
        }
    }

    public static int? ReadOptionalInt32(this ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int value))
        {
            return value;
        }

        reader.SkipValue();
        return null;
    }

    public static string ReadRequiredString(this ref Utf8JsonReader reader, string errorMessage)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new ServerVersionMismatchException(errorMessage);
        }

        return reader.GetString() ?? throw new ServerVersionMismatchException(errorMessage);
    }

    public static string? ReadOptionalString(this ref Utf8JsonReader reader, string errorMessage)
        => reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            _ => throw new ServerVersionMismatchException(errorMessage),
        };

    public static int ReadRequiredInt32(this ref Utf8JsonReader reader, string errorMessage)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int value))
        {
            throw new ServerVersionMismatchException(errorMessage);
        }

        return value;
    }

    public static DateTimeOffset? ReadOptionalDateTimeOffset(this ref Utf8JsonReader reader, string errorMessage)
        => reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String when reader.TryGetDateTimeOffset(out DateTimeOffset parsed) => parsed,
            _ => throw new ServerVersionMismatchException(errorMessage),
        };
}
