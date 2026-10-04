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

    public static byte[] SerializeRpcFailureResponse(IpcRpcFailureCode code) =>
        MemoryPackSerializer.Serialize(new IpcRpcFailureResponse(code));

    public static async Task<IpcRpcFailureResponse> ReadRpcFailureResponseAsync(
        Stream stream,
        int payloadLength,
        CancellationToken cancellationToken)
    {
        int expectedPayloadLength = SerializeRpcFailureResponse(IpcRpcFailureCode.Generic).Length;
        if (payloadLength != expectedPayloadLength)
        {
            throw new InvalidDataException(
                $"An IPC failure payload must be {expectedPayloadLength} bytes, got {payloadLength}.");
        }

        byte[] buffer = new byte[payloadLength];
        await stream.ReadExactlyAsync(buffer, cancellationToken);

        IpcRpcFailureResponse response = MemoryPackSerializer.Deserialize<IpcRpcFailureResponse>(buffer);
        if (!Enum.IsDefined(response.Code)
            || !SerializeRpcFailureResponse(response.Code).AsSpan().SequenceEqual(buffer))
        {
            throw new InvalidDataException("The IPC failure payload is invalid.");
        }

        return response;
    }

    public static async Task WriteRpcFailureResponseAsync(
        Stream stream,
        IpcRpcFailureCode code,
        CancellationToken cancellationToken)
    {
        byte[] payload = SerializeRpcFailureResponse(code);
        await new IpcRpcResponseHeader(IsSuccessful: false, payload.Length)
            .WriteAsync(stream, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
    }

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
