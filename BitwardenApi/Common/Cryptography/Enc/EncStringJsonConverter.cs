using System.Text;
using System.Text.Json.Serialization;

namespace BitwardenApi.Cryptography.Enc;

public sealed class EncStringJsonConverter : JsonConverter<EncString>
{
    public override EncString Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            throw new JsonException("Expected non-null EncString.");

        return EncString.CreateFrom(ref reader);
    }

    public override void Write(
        Utf8JsonWriter writer,
        EncString value,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException("Writing EncString values is not supported.");
    }
}
