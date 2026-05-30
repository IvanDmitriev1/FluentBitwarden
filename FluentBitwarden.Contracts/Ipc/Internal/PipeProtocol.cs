using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.HighPerformance.Buffers;

namespace FluentBitwarden.Contracts.Ipc.Internal;

internal static class PipeProtocol
{
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
        await header.WriteAsync(stream);
        await stream.WriteAsync(payloadWriter.WrittenMemory, cancellationToken);
    }

    public static async ValueTask WriteRequestMessageAsync(
        Stream stream,
        ushort messageType,
        CancellationToken cancellationToken)
    {
        RequestHeader header = new(messageType, PayloadLength: 0);
        await header.WriteAsync(stream);
    }

    public static async ValueTask WriteResponseMessageAsync<TMessage>(
        Stream stream,
        TMessage? message,
        CancellationToken cancellationToken)
        where TMessage : notnull
    {
        using ArrayPoolBufferWriter<byte> payloadWriter = new();
        MemoryPackSerializer.Serialize(payloadWriter, new IpcOptional<TMessage>(message));

        ResponseHeader header = new(payloadWriter.WrittenCount);
        await header.Write(stream);
        await stream.WriteAsync(payloadWriter.WrittenMemory, cancellationToken);
    }

    public static async ValueTask<TMessage> ReadRequestPayloadAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TMessage>(
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

    public static async ValueTask<TResponse?> ReadResponsePayloadAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        TResponse>(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
    {
        using var bufferOwner = MemoryOwner<byte>.Allocate(payloadLength);
        await stream.ReadExactlyAsync(bufferOwner.Memory, cancellationToken);

        var result = MemoryPackSerializer.Deserialize<IpcOptional<TResponse>>(bufferOwner.Memory.Span);
        return result.Value;
    }

}