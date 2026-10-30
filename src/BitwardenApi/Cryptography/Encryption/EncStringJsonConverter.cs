using System.Text.Json.Serialization;

namespace BitwardenApi.Cryptography.Encryption;

public sealed class EncStringJsonConverter : JsonConverter<EncString>
{
    public override EncString Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return EncString.Empty;

        return EncString.CreateFrom(ref reader);
    }

    public override void Write(
        Utf8JsonWriter writer,
        EncString value,
        JsonSerializerOptions options) =>
        value.WriteTo(writer);
}
