using System.Globalization;
using System.Text.Json.Serialization;

namespace BitwardenApi.Vault.Internal;
internal sealed class CollectionIdsJsonConverter : JsonConverter<CollectionId[]>
{
    public override CollectionId[] Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return [];

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Expected JSON array, got {reader.TokenType}.");

        List<CollectionId> collectionIds = [];
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return collectionIds.ToArray();

            if (reader.TokenType == JsonTokenType.Null)
            {
                collectionIds.Add(CollectionId.Empty);
                continue;
            }

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected collection id string, got {reader.TokenType}.");

            string? value = reader.GetString();
            collectionIds.Add(string.IsNullOrEmpty(value)
                ? CollectionId.Empty
                : CollectionId.Parse(value, CultureInfo.InvariantCulture));
        }

        throw new JsonException("Expected end of collectionIds array.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        CollectionId[] value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var collectionId in value)
        {
            if (collectionId.IsEmpty)
                continue;

            writer.WriteStringValue(collectionId.Value);
        }

        writer.WriteEndArray();
    }
}
