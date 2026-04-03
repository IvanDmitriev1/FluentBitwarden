namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponceParser
{
    public readonly record struct SyncParserReport(int Ciphers, int Folders, int Collections);

    private struct ArrayCaptureState
    {
        public bool IsActive { get; set; }
        public int ProcessedItems { get; set; }
    }

    private struct ObjectCaptureState
    {
        public bool IsActive { get; set; }
        public int Depth { get; set; }
    }

    public void ForwardToken(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject: _captureWriter.WriteStartObject(); break;
            case JsonTokenType.EndObject: _captureWriter.WriteEndObject(); break;
            case JsonTokenType.StartArray: _captureWriter.WriteStartArray(); break;
            case JsonTokenType.EndArray: _captureWriter.WriteEndArray(); break;
            case JsonTokenType.PropertyName: _captureWriter.WritePropertyName(reader.ValueSpan); break;
            case JsonTokenType.String: _captureWriter.WriteStringValue(reader.ValueSpan); break;
            case JsonTokenType.Number: _captureWriter.WriteRawValue(reader.ValueSpan); break;
            case JsonTokenType.True: _captureWriter.WriteBooleanValue(true); break;
            case JsonTokenType.False: _captureWriter.WriteBooleanValue(false); break;
            case JsonTokenType.Null: _captureWriter.WriteNullValue(); break;
        }
    }
}