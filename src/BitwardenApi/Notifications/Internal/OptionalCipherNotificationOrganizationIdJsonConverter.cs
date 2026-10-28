using System.Text.Json.Serialization;

namespace BitwardenApi.Notifications.Internal;

internal sealed class OptionalCipherNotificationOrganizationIdJsonConverter : JsonConverter<OrganizationId>
{
    public override bool HandleNull => true;

    public override OrganizationId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Null => OrganizationId.Empty,
            JsonTokenType.String => Parse(reader.GetString()),
            _ => throw new JsonException($"Expected JSON string, got {reader.TokenType}.")
        };

    public override void Write(Utf8JsonWriter writer, OrganizationId value, JsonSerializerOptions options)
    {
        if (value.IsEmpty)
        {

            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value);
        }
    }

    private static OrganizationId Parse(string? value) => string.IsNullOrEmpty(value) ? OrganizationId.Empty : OrganizationId.Parse(value);
}
