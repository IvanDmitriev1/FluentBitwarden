using System.Text.Json.Serialization;

namespace BitwardenApi.Notifications.Internal;

internal sealed class OptionalCipherNotificationUserIdJsonConverter : JsonConverter<UserId>
{
    public override bool HandleNull => true;

    public override UserId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Null => UserId.Empty,
            JsonTokenType.String => Parse(reader.GetString()),
            _ => throw new JsonException($"Expected JSON string, got {reader.TokenType}.")
        };

    public override void Write(Utf8JsonWriter writer, UserId value, JsonSerializerOptions options)
    {
        if (value == UserId.Empty) writer.WriteNullValue(); else writer.WriteStringValue(value.Value);
    }

    private static UserId Parse(string? value) => string.IsNullOrEmpty(value) ? UserId.Empty : UserId.Parse(value);
}
