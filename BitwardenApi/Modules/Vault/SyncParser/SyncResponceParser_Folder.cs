using BitwardenApi.Modules.Vault.Models;
using System.Text.Json;

namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponseParser
{
    private enum FolderProperty
    {
        None = 0,
        Id,
        Name,
        RevisionDate
    }

    private static void ParseFolder(
        ref Utf8JsonReader reader,
        int depth,
        ref FolderDto state,
        ref FolderProperty currentProperty)
    {
        if (reader.TokenType == JsonTokenType.PropertyName)
        {
            currentProperty = MatchFolderProperty(ref reader);
            return;
        }

        if (depth != 1)
        {
            return;
        }

        switch (currentProperty)
        {
            case FolderProperty.Id:
                state.Id = ParseFolderId(ref reader);
                break;
            case FolderProperty.Name:
                state.EncryptedName = ParseString(ref reader);
                break;
            case FolderProperty.RevisionDate:
                state.RevisionDate = ParseDateTimeOffset(ref reader);
                break;
        }
    }

    private static FolderProperty MatchFolderProperty(ref Utf8JsonReader reader)
    {
        if (reader.ValueTextEquals("id"u8) || reader.ValueTextEquals("Id"u8))
        {
            return FolderProperty.Id;
        }

        if (reader.ValueTextEquals("revisionDate"u8) || reader.ValueTextEquals("RevisionDate"u8))
        {
            return FolderProperty.RevisionDate;
        }

        if (reader.ValueTextEquals("name"u8) || reader.ValueTextEquals("Name"u8))
        {
            return FolderProperty.Name;
        }

        return FolderProperty.None;
    }
}
