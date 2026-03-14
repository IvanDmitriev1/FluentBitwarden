using System.Text.Json;

namespace BitwaredApi.Extensions;

internal static class Utf8JsonReaderExtensions
{
    public static void ReadRequiredStartObject(this ref Utf8JsonReader reader, string errorMessage)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException(errorMessage);
        }
    }

    public static void ReadNextTokenOrThrow(this ref Utf8JsonReader reader, string errorMessage)
    {
        if (!reader.Read())
        {
            throw new JsonException(errorMessage);
        }
    }

    public static void SkipValue(this ref Utf8JsonReader reader)
    {
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            reader.Skip();
        }
    }

    public static void EnsureNoTrailingData(
        this ref Utf8JsonReader reader,
        string errorMessage = "Encrypted cipher payload contained trailing data.")
    {
        if (reader.Read())
        {
            throw new JsonException(errorMessage);
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
}
