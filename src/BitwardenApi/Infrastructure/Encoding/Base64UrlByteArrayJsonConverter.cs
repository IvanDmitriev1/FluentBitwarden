using System.Buffers.Text;
using System.Text.Json.Serialization;

namespace BitwardenApi.Infrastructure.Encoding;

internal sealed class Base64UrlByteArrayJsonConverter : JsonConverter<byte[]>
{
    public override byte[] Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a Base64Url string.");
        }

        return Base64Url.DecodeFromUtf8(reader.ValueSpan);
    }

    public override void Write(
        Utf8JsonWriter writer,
        byte[] value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(Base64Url.EncodeToString(value));
    }
}
