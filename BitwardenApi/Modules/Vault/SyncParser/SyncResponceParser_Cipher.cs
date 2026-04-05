using BitwardenApi.Modules.Vault.Models;

namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponceParser
{
    private enum CipherProperty
    {
        None = 0,
        Id,
        FolderId,
        Type,
        Data
    }

    private struct CipherState
    {
        public CipherProperty CurrentProperty { get; set; }

        public CipherId? Id { get; set; }
        public FolderId? FolderId { get; set; }
        public CipherType? Type { get; set; }

        public int PayloadLength { get; set; }
    }

    private static void ParseCipher(ref Utf8JsonReader reader, ObjectCaptureState captureState, ref CipherState state)
    {
        if (reader.TokenType == JsonTokenType.PropertyName)
        {
            state.CurrentProperty = MatchCipherProperty(ref reader);
            return;
        }

        if (state.CurrentProperty == CipherProperty.None)
        {
            return;
        }

        Span<char> buffer = stackalloc char[64];
        int readBytes = 0;

        switch (state.CurrentProperty)
        {
            case CipherProperty.Id when captureState.Depth == 1 && reader.TokenType == JsonTokenType.String:
                readBytes = reader.CopyString(buffer);
                state.Id = CipherId.Parse(buffer[..readBytes]);
                break;
            case CipherProperty.FolderId when captureState.Depth == 1 && reader.TokenType == JsonTokenType.String:
                readBytes = reader.CopyString(buffer);
                state.FolderId = FolderId.Parse(buffer[..readBytes]);
                break;
            case CipherProperty.Type when captureState.Depth == 1 && reader.TokenType == JsonTokenType.Number:
                int type = reader.GetInt32();
                state.Type = (CipherType)type;
                break;
            case CipherProperty.Data when captureState.Depth == 1 && reader.TokenType == JsonTokenType.String:
            {
                captureState.ResizePayloadMemoryOwner(state.PayloadLength);

                reader.CopyString(captureState.PayloadSpan);
                state.PayloadLength = captureState.PayloadSpan.Length;

                break;
            }
        }
    }

    private static CipherProperty MatchCipherProperty(ref Utf8JsonReader reader)
    {
        if (reader.ValueTextEquals("id"u8))
        {
            return CipherProperty.Id;
        }

        if (reader.ValueTextEquals("folderId"u8))
        {
            return CipherProperty.FolderId;
        }

        if (reader.ValueTextEquals("type"u8))
        {
            return CipherProperty.Type;
        }

        if (reader.ValueTextEquals("data"u8))
        {
            return CipherProperty.Data;
        }

        return CipherProperty.None;
    }
}