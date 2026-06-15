using System.Text.Json;
using System.Text;

namespace FluentBitwarden.BrowserHost.Dispatching;

internal sealed record BrowserRequestEnvelope(string? Id, string? Type, JsonElement Payload)
{
    public static BrowserRequestEnvelope Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize(
                       json,
                       BrowserHostJsonContext.Default.BrowserRequestEnvelope)
                   ?? throw new BrowserJsonException("invalid_json", "Native message payload is not a JSON object.");
        }
        catch (JsonException)
        {
            throw new BrowserJsonException(
                "invalid_json",
                "Native message payload is not valid JSON.",
                TryReadId(json));
        }
    }

    private static string? TryReadId(string json)
    {
        try
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName &&
                    reader.ValueTextEquals("id") &&
                    reader.Read() &&
                    reader.TokenType == JsonTokenType.String)
                {
                    return reader.GetString();
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
