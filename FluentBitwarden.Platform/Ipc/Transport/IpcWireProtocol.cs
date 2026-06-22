using CommunityToolkit.HighPerformance.Buffers;

namespace FluentBitwarden.Platform.Ipc.Transport;

internal static class IpcWireProtocol
{
    public static async ValueTask WriteRpcRequestAsync<TMessage>(
        Stream stream,
        ushort messageType,
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : notnull
    {
        using ArrayPoolBufferWriter<byte> payloadWriter = new();
        MemoryPackSerializer.Serialize(payloadWriter, message);

        IpcMessageHeader header = new(messageType, payloadWriter.WrittenCount);
        await header.WriteAsync(stream, cancellationToken);
        await stream.WriteAsync(payloadWriter.WrittenMemory, cancellationToken);
    }

    public static ValueTask WriteRpcRequestAsync(
        Stream stream,
        ushort messageType,
        CancellationToken cancellationToken)
    {
        IpcMessageHeader header = new(messageType, PayloadLength: 0);
        return header.WriteAsync(stream, cancellationToken);
    }

    public static async ValueTask WriteRpcResponseAsync<TMessage>(
        Stream stream,
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : notnull
    {
        using ArrayPoolBufferWriter<byte> payloadWriter = new();
        MemoryPackSerializer.Serialize(payloadWriter, new IpcOptional<TMessage>(message));

        IpcRpcResponseHeader header = new(payloadWriter.WrittenCount);
        await header.WriteAsync(stream, cancellationToken);
        await stream.WriteAsync(payloadWriter.WrittenMemory, cancellationToken);
    }

    public static async ValueTask WriteEventAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        Stream stream,
        TEvent message,
        CancellationToken cancellationToken)
        where TEvent : IIpcEventMessage
    {
        using ArrayPoolBufferWriter<byte> payloadWriter = new();
        MemoryPackSerializer.Serialize(payloadWriter, message);

        IpcMessageHeader header = new(TEvent.MessageType, payloadWriter.WrittenCount);
        await header.WriteAsync(stream, cancellationToken);
        await stream.WriteAsync(payloadWriter.WrittenMemory, cancellationToken);
    }

    public static async ValueTask<TMessage> ReadMessagePayloadAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TMessage>(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
        where TMessage : notnull
    {
        using var bufferOwner = MemoryOwner<byte>.Allocate(payloadLength);
        await stream.ReadExactlyAsync(bufferOwner.Memory, cancellationToken);

        var message = MemoryPackSerializer.Deserialize<TMessage>(bufferOwner.Memory.Span);
        return message ?? throw new InvalidDataException("IPC message payload was null.");
    }

    public static async ValueTask<TResponse?> ReadRpcResponsePayloadAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
    {
        using var bufferOwner = MemoryOwner<byte>.Allocate(payloadLength);
        await stream.ReadExactlyAsync(bufferOwner.Memory, cancellationToken);

        var result = MemoryPackSerializer.Deserialize<IpcOptional<TResponse>>(bufferOwner.Memory.Span);
        return result.Value;
    }

    public static async ValueTask DiscardPayloadAsync(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
    {
        using var bufferOwner = MemoryOwner<byte>.Allocate(payloadLength);
        await stream.ReadExactlyAsync(bufferOwner.Memory, cancellationToken);
    }
}
