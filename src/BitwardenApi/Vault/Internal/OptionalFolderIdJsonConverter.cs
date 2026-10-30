using System.Globalization;
using System.Text.Json.Serialization;

namespace BitwardenApi.Vault.Internal;
internal sealed class OptionalFolderIdJsonConverter : JsonConverter<FolderId>
{
    public override FolderId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return FolderId.Empty;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected JSON string, got {reader.TokenType}.");

        string? value = reader.GetString();
        return string.IsNullOrEmpty(value)
            ? FolderId.Empty
            : FolderId.Parse(value, CultureInfo.InvariantCulture);
    }

    public override void Write(
        Utf8JsonWriter writer,
        FolderId value,
        JsonSerializerOptions options)
    {
        if (value.IsEmpty)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value);
    }
}
