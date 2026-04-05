using BitwardenApi.Modules.Vault.Abstractions;

namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponceParser
{
    private delegate void CaptureVisitor<TState>(
        ref Utf8JsonReader reader,
        ObjectCaptureState captureState,
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
            _objectCaptureState.PayloadSpan.Clear();

            state = default;
        }

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            _objectCaptureState.Depth++;
        }

        visitor.Invoke(ref reader, _objectCaptureState, ref state);

        if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
        {
            _objectCaptureState.Depth--;
        }

        if (_objectCaptureState.Depth == 0)
        {
            persist.Invoke(_dataWriter, ref state, _objectCaptureState.PayloadSpan);
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