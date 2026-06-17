using System.Text.Json.Serialization;

namespace BitwardenApi.Infrastructure.Serialization;

internal sealed class StringToUtf8BytesConverter : JsonConverter<byte[]>
{
    public override byte[] Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected JSON string, got {reader.TokenType}.");

        var value = reader.GetString() ?? throw new JsonException("Expected non-null string.");
        return System.Text.Encoding.UTF8.GetBytes(value);

    }

    public override void Write(
        Utf8JsonWriter writer,
        byte[] value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(System.Text.Encoding.UTF8.GetString(value));
    }
}
