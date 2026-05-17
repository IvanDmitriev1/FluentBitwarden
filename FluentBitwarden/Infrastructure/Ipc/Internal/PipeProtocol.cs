using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Infrastructure.Ipc.Abstractions;

namespace FluentBitwarden.Infrastructure.Ipc.Internal;

[Fody.ConfigureAwait(false)]
internal static class PipeProtocol
{
    public static async ValueTask<TMessage> ReadPayloadAsync<TMessage>(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
        where TMessage : IPipeMessage<TMessage>
    {
        using var bufferOwner = MemoryOwner<byte>.Allocate(payloadLength);
        await stream.ReadExactlyAsync(bufferOwner.Memory, cancellationToken);

        return TMessage.ReadPayload(bufferOwner.Memory.Span);
    }

    public static async ValueTask WriteResponseMessageAsync<TMessage>(
        Stream stream,
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : IPipeMessage<TMessage>
    {
        using ArrayPoolBufferWriter<byte> payloadWriter = new();
        message.WritePayload(payloadWriter);

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
