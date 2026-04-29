using CommunityToolkit.HighPerformance.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace FluentBitwarden.Shared.Ipc.Internal;

[Fody.ConfigureAwait(false)]
internal static class PipeProtocol
{
    public static async ValueTask<TMessage> ReadPayloadAsync<TMessage>(
        Stream stream,
        int payloadLength,
        JsonTypeInfo<TMessage> jsonTypeInfo,
        CancellationToken cancellationToken)
        where TMessage : notnull
    {
        using var bufferOwner = MemoryOwner<byte>.Allocate(payloadLength);
        await stream.ReadExactlyAsync(bufferOwner.Memory, cancellationToken);

        return JsonSerializer.Deserialize(bufferOwner.Memory.Span, jsonTypeInfo) ??
               throw new InvalidOperationException();
    }

    public static async ValueTask WriteResponseMessageAsync<TMessage>(
        Stream stream,
        TMessage message,
        JsonTypeInfo<TMessage> jsonTypeInfo,
        CancellationToken cancellationToken)
        where TMessage : notnull
    {
        using ArrayPoolBufferWriter<byte> payloadWriter = new();
        using var jsonWriter = new Utf8JsonWriter(payloadWriter);

        JsonSerializer.Serialize(jsonWriter, message, jsonTypeInfo);
        jsonWriter.Flush();

        ReadOnlyMemory<byte> payload = payloadWriter.WrittenMemory;
        if (payload.Length > IpcConstants.MaxPayloadLength)
        {
            throw new InvalidOperationException(
                $"IPC payload too large: {payload.Length} bytes.");
        }

        ResponseHeader header = new(payload.Length);
        header.Write(stream);

        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
