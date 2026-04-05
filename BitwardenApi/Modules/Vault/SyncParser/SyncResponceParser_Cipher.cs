using BitwardenApi.Modules.Vault.Models;
using System.Text.Json;

namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponseParser
{
    private enum CipherProperty
    {
        None = 0,
        Id,
        OrganizationId,
        FolderId,
        Key,
        Type,
        RevisionDate,
        CreationDate,
        DeletedDate,
        ArchivedDate,
        Favorite,
        Reprompt,
        Edit,
        ViewPassword,
        Data
    }

    private void ParseCipher(
        ref Utf8JsonReader reader,
        int depth,
        ref CipherDto state,
        ref CipherProperty currentProperty)
    {
        if (reader.TokenType == JsonTokenType.PropertyName)
        {
            currentProperty = MatchCipherProperty(ref reader);
            return;
        }

        if (depth != 1)
        {
            return;
        }

        switch (currentProperty)
        {
            case CipherProperty.Id:
                state.Id = ParseCipherId(ref reader);
                break;
            case CipherProperty.OrganizationId:
                state.OrganizationId = ParseNullableOrganizationId(ref reader);
                break;
            case CipherProperty.FolderId:
                state.FolderId = reader.TokenType == JsonTokenType.Null ? null : ParseFolderId(ref reader);
                break;
            case CipherProperty.Key:
                state.EncryptedKey = reader.TokenType == JsonTokenType.Null ? null : ParseString(ref reader);
                break;
            case CipherProperty.Type:
                state.CipherType = (CipherType)ParseInt(ref reader);
                break;
            case CipherProperty.RevisionDate:
                state.RevisionDate = ParseDateTimeOffset(ref reader);
                break;
            case CipherProperty.CreationDate:
                state.CreationDate = ParseDateTimeOffset(ref reader);
                break;
            case CipherProperty.DeletedDate:
                state.DeletedDate = ParseNullableDateTimeOffset(ref reader);
                break;
            case CipherProperty.ArchivedDate:
                state.ArchivedDate = ParseNullableDateTimeOffset(ref reader);
                break;
            case CipherProperty.Favorite:
                state.Favorite = ParseBooleanOrIntFlag(ref reader);
                break;
            case CipherProperty.Reprompt:
                state.Reprompt = ParseBooleanOrIntFlag(ref reader);
                break;
            case CipherProperty.Edit:
                state.Edit = ParseBooleanOrIntFlag(ref reader);
                break;
            case CipherProperty.ViewPassword:
                state.ViewPassword = ParseBooleanOrIntFlag(ref reader);
                break;
            case CipherProperty.Data:
                if (reader.TokenType == JsonTokenType.String)
                {
                    _cipherPayloadCapture.CaptureDecodedStringPayload(ref reader);
                }
                break;
        }
    }

    private static CipherProperty MatchCipherProperty(ref Utf8JsonReader reader)
    {
        if (reader.ValueTextEquals("id"u8) || reader.ValueTextEquals("Id"u8))
        {
            return CipherProperty.Id;
        }

        if (reader.ValueTextEquals("organizationId"u8) || reader.ValueTextEquals("OrganizationId"u8))
        {
            return CipherProperty.OrganizationId;
        }

        if (reader.ValueTextEquals("folderId"u8) || reader.ValueTextEquals("FolderId"u8))
        {
            return CipherProperty.FolderId;
        }

        if (reader.ValueTextEquals("key"u8) || reader.ValueTextEquals("Key"u8))
        {
            return CipherProperty.Key;
        }

        if (reader.ValueTextEquals("type"u8) || reader.ValueTextEquals("Type"u8))
        {
            return CipherProperty.Type;
        }

        if (reader.ValueTextEquals("revisionDate"u8) || reader.ValueTextEquals("RevisionDate"u8))
        {
            return CipherProperty.RevisionDate;
        }

        if (reader.ValueTextEquals("creationDate"u8) || reader.ValueTextEquals("CreationDate"u8))
        {
            return CipherProperty.CreationDate;
        }

        if (reader.ValueTextEquals("deletedDate"u8) || reader.ValueTextEquals("DeletedDate"u8))
        {
            return CipherProperty.DeletedDate;
        }

        if (reader.ValueTextEquals("archivedDate"u8) || reader.ValueTextEquals("ArchivedDate"u8))
        {
            return CipherProperty.ArchivedDate;
        }

        if (reader.ValueTextEquals("favorite"u8) || reader.ValueTextEquals("Favorite"u8))
        {
            return CipherProperty.Favorite;
        }

        if (reader.ValueTextEquals("reprompt"u8) || reader.ValueTextEquals("Reprompt"u8))
        {
            return CipherProperty.Reprompt;
        }

        if (reader.ValueTextEquals("edit"u8) || reader.ValueTextEquals("Edit"u8))
        {
            return CipherProperty.Edit;
        }

        if (reader.ValueTextEquals("viewPassword"u8) || reader.ValueTextEquals("ViewPassword"u8))
        {
            return CipherProperty.ViewPassword;
        }

        if (reader.ValueTextEquals("data"u8) || reader.ValueTextEquals("Data"u8))
        {
            return CipherProperty.Data;
        }

        return CipherProperty.None;
    }
}
