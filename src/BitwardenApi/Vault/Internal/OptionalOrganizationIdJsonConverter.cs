using System.Globalization;
using System.Text.Json.Serialization;

namespace BitwardenApi.Vault.Internal;
internal sealed class OptionalOrganizationIdJsonConverter : JsonConverter<OrganizationId>
{
    public override OrganizationId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return OrganizationId.Empty;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected JSON string, got {reader.TokenType}.");

        string? value = reader.GetString();
        return string.IsNullOrEmpty(value)
            ? OrganizationId.Empty
            : OrganizationId.Parse(value, CultureInfo.InvariantCulture);
    }

    public override void Write(
        Utf8JsonWriter writer,
        OrganizationId value,
        JsonSerializerOptions options)
    {
        if (value.IsEmpty)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value);
    }
}
