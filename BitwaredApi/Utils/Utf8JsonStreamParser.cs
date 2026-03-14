using System.Text.Json;

namespace BitwaredApi.Utils;

internal delegate void Utf8JsonPassProcessor<TState>(
    TState state,
    ref Utf8JsonReader reader,
    ReadOnlySpan<byte> buffer);

internal delegate void Utf8JsonPassCompleted<TState>(
    TState state,
    ReadOnlySpan<byte> buffer,
    long bytesConsumed);

internal delegate ValueTask Utf8JsonPassCompletedAsync<TState>(
    TState state,
    ReadOnlyMemory<byte> buffer,
    long bytesConsumed,
    CancellationToken cancellationToken);

internal static class Utf8JsonStreamParser
{
    public static void Parse<TState>(
        Stream stream,
        TState state,
        Utf8JsonPassProcessor<TState> processPass,
        Utf8JsonPassCompleted<TState>? onPassCompleted = null)
    {
        using Utf8JsonStreamReader streamReader = new(stream);

        while (!streamReader.IsFinalBlock || streamReader.HasBufferedData)
        {
            if (!streamReader.IsFinalBlock)
            {
                streamReader.ReadMore();
            }

            ReadOnlySpan<byte> buffer = streamReader.WrittenSpan;
            Utf8JsonReader reader = streamReader.CreateReader();

            processPass(state, ref reader, buffer);
            onPassCompleted?.Invoke(state, buffer, reader.BytesConsumed);
            streamReader.Advance(reader);
        }
    }

    public static async ValueTask ParseAsync<TState>(
        Stream stream,
        TState state,
        Utf8JsonPassProcessor<TState> processPass,
        Utf8JsonPassCompletedAsync<TState>? onPassCompletedAsync = null,
        CancellationToken cancellationToken = default)
    {
        using Utf8JsonStreamReader streamReader = new(stream);

        while (!streamReader.IsFinalBlock || streamReader.HasBufferedData)
        {
            if (!streamReader.IsFinalBlock)
            {
                await streamReader.ReadMoreAsync(cancellationToken).ConfigureAwait(false);
            }

            ReadOnlyMemory<byte> buffer = streamReader.WrittenMemory;
            Utf8JsonReader reader = streamReader.CreateReader();

            processPass(state, ref reader, buffer.Span);
            long bytesConsumed = reader.BytesConsumed;
            JsonReaderState readerState = reader.CurrentState;

            if (onPassCompletedAsync is not null)
            {
                await onPassCompletedAsync(state, buffer, bytesConsumed, cancellationToken)
                    .ConfigureAwait(false);
            }

            streamReader.Advance(bytesConsumed, readerState);
        }
    }

    public static void UpdateDepth(ref int depth, JsonTokenType tokenType)
    {
        switch (tokenType)
        {
            case JsonTokenType.StartObject:
            case JsonTokenType.StartArray:
                depth++;
                break;

            case JsonTokenType.EndObject:
            case JsonTokenType.EndArray:
                depth--;
                break;
        }
    }
}
