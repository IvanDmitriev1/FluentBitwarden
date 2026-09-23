namespace FluentBitwarden.Platform.Ipc.Transport;

internal static class IpcWireProtocol
{
    public static async Task WriteRpcRequestAsync<TMessage>(
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

    public static byte[] SerializeRpcResponse<TMessage>(TMessage message)
        where TMessage : notnull
        => MemoryPackSerializer.Serialize(new IpcOptional<TMessage>(message));

    public static async Task WriteRpcResponseAsync(
        Stream stream,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        IpcRpcResponseHeader header = new(IsSuccessful: true, payload.Length);
        await header.WriteAsync(stream, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
    }

    public static Task WriteRpcFailureResponseAsync(
        Stream stream,
        CancellationToken cancellationToken) =>
        new IpcRpcResponseHeader(IsSuccessful: false, PayloadLength: 0)
            .WriteAsync(stream, cancellationToken);

    public static async Task WriteEventAsync<
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

    public static Task<TMessage> ReadMessagePayloadAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        TMessage>(byte[] payload, CancellationToken cancellationToken)
        where TMessage : notnull
    {
        var message = MemoryPackSerializer.Deserialize<TMessage>(payload);
        return Task.FromResult(
            message ?? throw new InvalidDataException("IPC message payload was null."));
    }

    public static async Task<TResponse?> ReadRpcResponsePayloadAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        TResponse>(
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
