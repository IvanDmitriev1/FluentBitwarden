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
        byte[] payload = MemoryPackSerializer.Serialize(message);

        IpcMessageHeader header = new(messageType, payload.Length);
        await header.WriteAsync(stream, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
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
        byte[] payload = MemoryPackSerializer.Serialize(new IpcOptional<TMessage>(message));

        IpcRpcResponseHeader header = new(payload.Length);
        await header.WriteAsync(stream, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
    }

    public static async ValueTask WriteEventAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        Stream stream,
        TEvent message,
        CancellationToken cancellationToken)
        where TEvent : IIpcEventMessage
    {
        byte[] payload = MemoryPackSerializer.Serialize(message);

        IpcMessageHeader header = new(TEvent.MessageType, payload.Length);
        await header.WriteAsync(stream, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
    }

    public static async ValueTask<TMessage> ReadMessagePayloadAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TMessage>(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
        where TMessage : notnull
    {
        byte[] buffer = new byte[payloadLength];
        await stream.ReadExactlyAsync(buffer, cancellationToken);

        var message = MemoryPackSerializer.Deserialize<TMessage>(buffer);
        return message ?? throw new InvalidDataException("IPC message payload was null.");
    }

    public static async ValueTask<TResponse?> ReadRpcResponsePayloadAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[payloadLength];
        await stream.ReadExactlyAsync(buffer, cancellationToken);

        var result = MemoryPackSerializer.Deserialize<IpcOptional<TResponse>>(buffer);
        return result.Value;
    }
}