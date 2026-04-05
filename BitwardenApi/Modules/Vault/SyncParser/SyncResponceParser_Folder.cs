using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponceParser
{
    private enum FolderProperty
    {
        None = 0,
        Id,
        Type
    }

    private struct FolderState
    {
        public FolderProperty CurrentProperty { get; set; }
        public FolderId? Id { get; set; }
    }

    private static void ParseFolder(ref Utf8JsonReader reader, ObjectCaptureState objectCaptureState, ref FolderState state)
    {
        if (reader.TokenType == JsonTokenType.PropertyName)
        {
            state.CurrentProperty = MatchFolderProperty(ref reader);
            return;
        }

        if (state.CurrentProperty == FolderProperty.None)
            return;

        Span<char> buffer = stackalloc char[64];
        int readBytes = 0;

        switch (state.CurrentProperty)
        {
            case FolderProperty.Id when reader.TokenType == JsonTokenType.String:
                readBytes = reader.CopyString(buffer);
                state.Id = FolderId.Parse(buffer[..readBytes]);
                break;
        }
    }

    private static FolderProperty MatchFolderProperty(ref Utf8JsonReader reader)
    {
        if (reader.ValueTextEquals("id"u8))
        {
            return FolderProperty.Id;
        }

        return FolderProperty.None;
    }
}