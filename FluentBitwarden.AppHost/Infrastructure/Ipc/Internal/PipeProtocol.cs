using CommunityToolkit.HighPerformance.Buffers;
using MemoryPack;

namespace FluentBitwarden.Infrastructure.Ipc.Internal;

[Fody.ConfigureAwait(false)]
internal static class PipeProtocol
{
    public static async ValueTask<TMessage> ReadPayloadAsync<TMessage>(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
        where TMessage : notnull
    {
        using var bufferOwner = MemoryOwner<byte>.Allocate(payloadLength);
        await stream.ReadExactlyAsync(bufferOwner.Memory, cancellationToken);

        var message = MemoryPackSerializer.Deserialize<TMessage>(bufferOwner.Memory.Span);
        return message ?? throw new InvalidOperationException("IPC payload deserialized to null.");
    }

    public static async ValueTask WriteRequestMessageAsync<TMessage>(
        Stream stream,
        ushort messageType,
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : notnull
    {
        using ArrayPoolBufferWriter<byte> payloadWriter = new();
        MemoryPackSerializer.Serialize(payloadWriter, message);

        RequestHeader header = new(messageType, payloadWriter.WrittenCount);
        header.Write(stream);

        await stream.WriteAsync(payloadWriter.WrittenMemory, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async ValueTask WriteResponseMessageAsync<TMessage>(
        Stream stream,
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : notnull
    {
        using ArrayPoolBufferWriter<byte> payloadWriter = new();
        MemoryPackSerializer.Serialize(payloadWriter, message);

        ResponseHeader header = new(payloadWriter.WrittenCount);
        header.Write(stream);

        await stream.WriteAsync(payloadWriter.WrittenMemory, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
