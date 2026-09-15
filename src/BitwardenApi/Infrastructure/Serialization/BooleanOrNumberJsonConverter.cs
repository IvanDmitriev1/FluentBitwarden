using System.Text.Json.Serialization;

namespace BitwardenApi.Infrastructure.Serialization;

internal sealed class BooleanOrNumberJsonConverter : JsonConverter<bool>
{
    public override bool Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,

            JsonTokenType.Number => reader.GetInt32() != 0,

            _ => throw new JsonException(
                $"Expected boolean or numeric flag, got {reader.TokenType}.")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        bool value,
        JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
