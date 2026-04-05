using BitwardenApi.Modules.Vault.Models;
using System.Text.Json;

namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponseParser
{
    private enum CollectionProperty
    {
        None = 0,
        Id,
        Name,
        OrganizationId,
        ReadOnly,
        Manage,
        HidePasswords,
        Type
    }

    private static void ParseCollection(
        ref Utf8JsonReader reader,
        int depth,
        ref CollectionDto state,
        ref CollectionProperty currentProperty)
    {
        if (reader.TokenType == JsonTokenType.PropertyName)
        {
            currentProperty = MatchCollectionProperty(ref reader);
            return;
        }

        if (depth != 1)
        {
            return;
        }

        switch (currentProperty)
        {
            case CollectionProperty.Id:
                state.Id = ParseCollectionId(ref reader);
                break;
            case CollectionProperty.Name:
                state.EncryptedName = ParseString(ref reader);
                break;
            case CollectionProperty.OrganizationId:
                state.OrganizationId = ParseNullableOrganizationId(ref reader);
                break;
            case CollectionProperty.ReadOnly:
                state.ReadOnly = ParseBooleanOrIntFlag(ref reader);
                break;
            case CollectionProperty.Manage:
                state.Manage = ParseBooleanOrIntFlag(ref reader);
                break;
            case CollectionProperty.HidePasswords:
                state.HidePasswords = ParseBooleanOrIntFlag(ref reader);
                break;
            case CollectionProperty.Type:
                state.Type = ParseNullableInt(ref reader);
                break;
        }
    }

    private static CollectionProperty MatchCollectionProperty(ref Utf8JsonReader reader)
    {
        if (reader.ValueTextEquals("id"u8) || reader.ValueTextEquals("Id"u8))
        {
            return CollectionProperty.Id;
        }

        if (reader.ValueTextEquals("organizationId"u8) || reader.ValueTextEquals("OrganizationId"u8))
        {
            return CollectionProperty.OrganizationId;
        }

        if (reader.ValueTextEquals("name"u8) || reader.ValueTextEquals("Name"u8))
        {
            return CollectionProperty.Name;
        }

        if (reader.ValueTextEquals("readOnly"u8) || reader.ValueTextEquals("ReadOnly"u8))
        {
            return CollectionProperty.ReadOnly;
        }

        if (reader.ValueTextEquals("manage"u8) || reader.ValueTextEquals("Manage"u8))
        {
            return CollectionProperty.Manage;
        }

        if (reader.ValueTextEquals("hidePasswords"u8) || reader.ValueTextEquals("HidePasswords"u8))
        {
            return CollectionProperty.HidePasswords;
        }

        if (reader.ValueTextEquals("type"u8) || reader.ValueTextEquals("Type"u8))
        {
            return CollectionProperty.Type;
        }

        return CollectionProperty.None;
    }
}
