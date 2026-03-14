using System.Text;
using System.Text.Json;
using BitwaredApi.Abstractions.Exceptions;
using BitwaredApi.Models.Vault;

namespace BitwaredApi.Extensions;

internal static class JsonElementExtensions
{
    public static void AddItemsTo<TItem>(
        this JsonElement section,
        string propertyName,
        List<TItem> items,
        Func<JsonElement, TItem> createItem)
    {
        foreach (JsonElement item in section.EnumerateRequiredObjectArray(propertyName))
        {
            items.Add(createItem(item));
        }
    }

    public static IEnumerable<JsonElement> EnumerateRequiredObjectArray(this JsonElement section, string propertyName)
    {
        if (section.ValueKind == JsonValueKind.Null)
        {
            yield break;
        }

        if (section.ValueKind != JsonValueKind.Array)
        {
            throw new ServerVersionMismatchException($"Sync response property '{propertyName}' was not an array.");
        }

        foreach (JsonElement item in section.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new ServerVersionMismatchException("Sync response section contained a non-object item.");
            }

            yield return item;
        }
    }

    public static CipherSyncItem ToCipherSyncItem(this JsonElement item)
        => new(
            item.ReadRequiredId("cipher", "ciphers"),
            item.ReadRequiredType(),
            item.ReadOptionalString("organizationId", "Sync cipher payload property 'organizationId' was not a string."),
            item.ReadOptionalString("folderId", "Sync cipher payload property 'folderId' was not a string."),
            item.ReadCollectionIdsJson(),
            item.ReadRevisionDate("ciphers"),
            item.GetPayloadBytes());

    public static FolderSyncItem ToFolderSyncItem(this JsonElement item)
        => new(
            item.ReadRequiredId("folder", "folders"),
            item.ReadRevisionDate("folders"),
            item.GetPayloadBytes());

    public static CollectionSyncItem ToCollectionSyncItem(this JsonElement item)
        => new(
            item.ReadRequiredId("collection", "collections"),
            item.ReadRevisionDate("collections"),
            item.GetPayloadBytes());

    public static string ReadRequiredId(this JsonElement item, string itemName, string sectionName)
    {
        if (!item.TryGetProperty("id", out JsonElement property))
        {
            throw new ServerVersionMismatchException($"Sync {itemName} payload did not include an Id.");
        }

        return property.ReadRequiredString($"Sync {sectionName} payload property 'id' was not a string.");
    }

    public static int ReadRequiredType(this JsonElement item)
    {
        if (!item.TryGetProperty("type", out JsonElement property))
        {
            throw new ServerVersionMismatchException("Sync cipher payload did not include a type.");
        }

        return property.ReadRequiredInt32("Sync cipher payload property 'type' was not a number.");
    }

    public static string ReadCollectionIdsJson(this JsonElement item)
    {
        if (!item.TryGetProperty("collectionIds", out JsonElement property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return "[]";
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw new ServerVersionMismatchException("Sync cipher payload property 'collectionIds' was not an array.");
        }

        foreach (JsonElement collectionId in property.EnumerateArray())
        {
            if (collectionId.ValueKind != JsonValueKind.String)
            {
                throw new ServerVersionMismatchException(
                    "Sync cipher payload property 'collectionIds' contained a non-string value.");
            }
        }

        return property.GetRawText();
    }

    public static DateTimeOffset? ReadRevisionDate(this JsonElement item, string sectionName)
        => item.ReadOptionalDateTimeOffset(
            "revisionDate",
            $"Sync {sectionName} payload property 'revisionDate' was not a valid date-time string.");

    public static byte[] GetPayloadBytes(this JsonElement item)
        => Encoding.UTF8.GetBytes(item.GetRawText());

    public static string ReadRequiredString(this JsonElement property, string errorMessage)
    {
        if (property.ValueKind != JsonValueKind.String)
        {
            throw new ServerVersionMismatchException(errorMessage);
        }

        return property.GetString() ?? throw new ServerVersionMismatchException(errorMessage);
    }

    public static string? ReadOptionalString(this JsonElement item, string propertyName, string errorMessage)
    {
        if (!item.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => property.GetString(),
            _ => throw new ServerVersionMismatchException(errorMessage),
        };
    }

    public static int ReadRequiredInt32(this JsonElement property, string errorMessage)
    {
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out int value))
        {
            throw new ServerVersionMismatchException(errorMessage);
        }

        return value;
    }

    public static DateTimeOffset? ReadOptionalDateTimeOffset(
        this JsonElement item,
        string propertyName,
        string errorMessage)
    {
        if (!item.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String when property.TryGetDateTimeOffset(out DateTimeOffset parsed) => parsed,
            _ => throw new ServerVersionMismatchException(errorMessage),
        };
    }
}
