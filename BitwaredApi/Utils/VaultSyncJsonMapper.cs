using System.Buffers;
using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Extensions;
using BitwaredApi.Models.Vault;
using BitwaredApi.Utils;

namespace BitwaredApi.Utils;

internal static class VaultSyncJsonMapper
{
    public static EncryptedCipherRecord MapCipher(JsonElement item, string accountId, DateTimeOffset updatedUtc)
        => new(
            accountId,
            item.GetRequiredStringProperty("id", "Sync cipher payload did not include an Id."),
            item.GetRequiredInt32Property("type", "Sync cipher payload did not include a type."),
            item.GetOptionalStringProperty("organizationId"),
            item.GetOptionalStringProperty("folderId"),
            ReadCollectionIdsJson(item),
            item.GetOptionalDateTimeOffsetProperty("revisionDate", "Sync payload property 'revisionDate' was not a string."),
            SerializeToUtf8(item),
            updatedUtc);

    public static EncryptedFolderRecord MapFolder(JsonElement item, string accountId, DateTimeOffset updatedUtc)
        => new(
            accountId,
            item.GetRequiredStringProperty("id", "Sync folder payload did not include an Id."),
            item.GetOptionalDateTimeOffsetProperty("revisionDate", "Sync payload property 'revisionDate' was not a string."),
            SerializeToUtf8(item),
            updatedUtc);

    public static EncryptedCollectionRecord MapCollection(JsonElement item, string accountId, DateTimeOffset updatedUtc)
        => new(
            accountId,
            item.GetRequiredStringProperty("id", "Sync collection payload did not include an Id."),
            item.GetOptionalDateTimeOffsetProperty("revisionDate", "Sync payload property 'revisionDate' was not a string."),
            SerializeToUtf8(item),
            updatedUtc);

    private static byte[] SerializeToUtf8(JsonElement element)
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        using Utf8JsonWriter writer = new(bufferWriter);
        element.WriteTo(writer);
        writer.Flush();
        return bufferWriter.WrittenMemory.ToArray();
    }

    private static string ReadCollectionIdsJson(JsonElement element)
    {
        if (!element.TryGetProperty("collectionIds", out JsonElement property)
            || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return "[]";
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw new ServerVersionMismatchException("Sync cipher payload property 'collectionIds' was not an array.");
        }

        string[] values = property.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String
                ? item.GetString() ?? string.Empty
                : throw new ServerVersionMismatchException("Sync cipher payload property 'collectionIds' contained a non-string value."))
            .ToArray();

        return JsonSerializer.Serialize(values, JsonDefaults.SerializerOptions);
    }
}
