using BitwardenApi.Modules.Vault.Abstractions;

namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponceParser
{
    private delegate void CaptureVisitor<TState>(
        ref Utf8JsonReader reader,
        int depth,
        ref TState state)
        where TState : struct;

    private delegate void PersistVisitor<TState>(
        ISyncDataWriter writer,
        ref TState state,
        ReadOnlySpan<byte> payload)
        where TState : struct;


    private void CaptureCurrentObject<TState>(
        ref Utf8JsonReader reader,
        ref TState state,
        CaptureVisitor<TState> visitor,
        PersistVisitor<TState> persist) where TState : struct
    {
        if (!_objectCaptureState.IsActive)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected StartObject, got {reader.TokenType}.");
            }

            _objectCaptureState.IsActive = true;
            _objectCaptureState.Depth = 0;

            state = default;
            _captureWriter.Reset();
        }

        ForwardToken(ref reader);

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            _objectCaptureState.Depth++;
        }

        visitor.Invoke(ref reader, _objectCaptureState.Depth, ref state);

        if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
        {
            _objectCaptureState.Depth--;
        }

        if (_objectCaptureState.Depth == 0)
        {
            _captureWriter.Flush();
            persist.Invoke(_dataWriter, ref state, _captureBuffer.WrittenSpan);
            _objectCaptureState.IsActive = false;
        }
    }


    private void CaptureArray<TState>(
        ref Utf8JsonReader reader,
        ref TState state,
        CaptureVisitor<TState> visitor, 
        PersistVisitor<TState> persist) where TState : struct
    {
        if (!_arrayCaptureState.IsActive)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException($"Expected StartArray, got {reader.TokenType}.");
            }

            _arrayCaptureState.IsActive = true;
            _arrayCaptureState.ProcessedItems = 0;
            return;
        }

        if (reader.TokenType == JsonTokenType.EndArray && !_objectCaptureState.IsActive)
        {
            _arrayCaptureState = default;
            return;
        }

        if (reader.TokenType == JsonTokenType.StartObject && !_objectCaptureState.IsActive)
        {
            _arrayCaptureState.ProcessedItems++;
        }

        CaptureCurrentObject(ref reader, ref state, visitor, persist);
    }
}