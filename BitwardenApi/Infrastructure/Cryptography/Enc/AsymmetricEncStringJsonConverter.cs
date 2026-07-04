using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitwardenApi.Infrastructure.Cryptography.Enc;

public sealed class AsymmetricEncStringJsonConverter : JsonConverter<AsymmetricEncString>
{
    public override AsymmetricEncString Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return AsymmetricEncString.Empty;

        return AsymmetricEncString.FromEncString(EncString.CreateFrom(ref reader));
    }

    public override void Write(
        Utf8JsonWriter writer,
        AsymmetricEncString value,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException("Writing AsymmetricEncString values is not supported.");
    }
}
