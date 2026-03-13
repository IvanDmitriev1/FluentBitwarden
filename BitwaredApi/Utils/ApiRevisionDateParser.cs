using System.Globalization;
using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Serialization;

namespace BitwaredApi.Utils;

internal static class ApiRevisionDateParser
{
    public static DateTimeOffset? Parse(string body, string? mediaType)
    {
        string trimmedBody = body.Trim();
        if (string.IsNullOrWhiteSpace(trimmedBody) || string.Equals(trimmedBody, "null", StringComparison.Ordinal))
        {
            return null;
        }

        JsonException? jsonException = null;
        if (LooksLikeJson(trimmedBody))
        {
            try
            {
                return ParseJsonValue(trimmedBody);
            }
            catch (JsonException ex)
            {
                jsonException = ex;
            }
        }

        string unwrappedBody = UnwrapSingleQuotedValue(trimmedBody);
        if (TryParseUnixMilliseconds(unwrappedBody, out DateTimeOffset unixMillisecondsParsed))
        {
            return unixMillisecondsParsed;
        }

        if (DateTimeOffset.TryParseExact(
            unwrappedBody,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
            out DateTimeOffset exactParsed))
        {
            return exactParsed;
        }

        if (DateTimeOffset.TryParse(
            unwrappedBody,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
            out DateTimeOffset parsed))
        {
            return parsed;
        }

        throw new ServerVersionMismatchException(
            $"Revision date endpoint returned an unsupported value. Content-Type: '{mediaType ?? "<none>"}'. Body: {trimmedBody}",
            jsonException);
    }

    private static DateTimeOffset? ParseJsonValue(string body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        return root.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => JsonSerializer.Deserialize(body, BitwaredApiJsonContext.Default.NullableDateTimeOffset),
            JsonValueKind.Number when root.TryGetInt64(out long unixMilliseconds) => DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds),
            JsonValueKind.Number => throw new JsonException("Revision date number was not an integer."),
            _ => throw new JsonException($"Revision date JSON value kind '{root.ValueKind}' is not supported."),
        };
    }

    private static bool LooksLikeJson(string value)
        => value.Length > 0
           && (value[0] == '"'
               || char.IsDigit(value[0])
               || value[0] == '-'
               || value[0] == '{'
               || value[0] == '['
               || string.Equals(value, "null", StringComparison.Ordinal));

    private static string UnwrapSingleQuotedValue(string value)
        => value.Length >= 2 && value[0] == '"' && value[^1] == '"'
            ? value[1..^1]
            : value;

    private static bool TryParseUnixMilliseconds(string value, out DateTimeOffset parsed)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unixMilliseconds))
        {
            parsed = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
            return true;
        }

        parsed = default;
        return false;
    }
}
