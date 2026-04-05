using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponceParser
{
    private enum CollectionProperty
    {
        None = 0,
        Id,
        Type
    }

    private struct CollectionState
    {
        public CollectionProperty CurrentProperty { get; set; }
        public CollectionId? Id { get; set; }
        public int? Type { get; set; }
    }

    private static void ParseCollection(ref Utf8JsonReader reader, ObjectCaptureState objectCaptureState, ref CollectionState state)
    {
        if (reader.TokenType == JsonTokenType.PropertyName)
        {
            state.CurrentProperty = MatchCollectionProperty(ref reader);
            return;
        }

        if (state.CurrentProperty == CollectionProperty.None)
            return;

        Span<char> buffer = stackalloc char[64];
        int readBytes = 0;

        switch (state.CurrentProperty)
        {
            case CollectionProperty.Id when reader.TokenType == JsonTokenType.String:
                readBytes = reader.CopyString(buffer);
                state.Id = CollectionId.Parse(buffer[..readBytes]);
                break;
        }
    }

    private static CollectionProperty MatchCollectionProperty(ref Utf8JsonReader reader)
    {
        if (reader.ValueTextEquals("id"u8))
        {
            return CollectionProperty.Id;
        }

        return CollectionProperty.None;
    }
}