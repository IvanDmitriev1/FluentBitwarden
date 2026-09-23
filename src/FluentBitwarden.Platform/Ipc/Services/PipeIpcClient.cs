using FluentBitwarden.Platform.Ipc.Transport;
using System.IO.Pipes;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcClient(string pipeName) : IIpcClient
{
    public async Task<TResponse> SendAsync<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IIpcRequestMessage
    {
        await using var pipe = CreatePipeClient();
        await pipe.ConnectAsync(cancellationToken);

        await IpcWireProtocol.WriteRpcRequestAsync(
            pipe,
            TRequest.MessageType,
            request,
            cancellationToken);

        return await ReadResponseAsync<TResponse>(pipe, cancellationToken);
    }

    private static async Task<TResponse> ReadResponseAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    TResponse>(
        Stream pipe,
        CancellationToken cancellationToken)
    {
        var responseHeader = await IpcRpcResponseHeader.ReadAsync(pipe, cancellationToken);
        if (!responseHeader.IsSuccessful)
            throw new OperationCanceledException();

        return (await IpcWireProtocol.ReadRpcResponsePayloadAsync<TResponse>(
            pipe,
            responseHeader.PayloadLength,
            cancellationToken))!;
    }

    private NamedPipeClientStream CreatePipeClient() => new(
        ".",
        pipeName,
        PipeDirection.InOut,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
}
