using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.HighPerformance.Buffers;

namespace FluentBitwarden.Contracts.Ipc.Internal;

internal static class PipeProtocol
{
    public static async ValueTask<TMessage> ReadPayloadAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TMessage>(
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

    public static async ValueTask WriteRequestMessageAsync(
        Stream stream,
        ushort messageType,
        CancellationToken cancellationToken)
    {
        RequestHeader header = new(messageType, PayloadLength: 0);
        header.Write(stream);

        await stream.FlushAsync(cancellationToken);
    }


    public static ValueTask WriteSuccessResponseMessageAsync<TMessage>(
        Stream stream,
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : notnull =>
        WriteResponseMessageAsync(stream, IpcResult<TMessage>.Success(message), cancellationToken);

    public static ValueTask WriteFailureResponseMessageAsync<TMessage>(
        Stream stream,
        string error,
        CancellationToken cancellationToken)
        where TMessage : notnull =>
        WriteResponseMessageAsync(stream, IpcResult<TMessage>.Failure(error), cancellationToken);

    private static async ValueTask WriteResponseMessageAsync<TMessage>(
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
