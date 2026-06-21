using FluentBitwarden.Platform.Ipc.Transport;
using System.IO.Pipes;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcClient(string pipeName) : IIpcClient
{
    public async ValueTask<TResponse> SendAsync<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IIpcRequestMessage
    {
        await using var pipe = CreatePipeClient();
        await pipe.ConnectAsync(cancellationToken);

        await PipeProtocol.WriteRequestMessageAsync(
            pipe,
            TRequest.MessageType,
            request,
            cancellationToken);

        var responseHeader = await ResponseHeader.ReadAsync(pipe);
        return (await PipeProtocol.ReadResponsePayloadAsync<TResponse>(
            pipe,
            responseHeader.PayloadLength,
            cancellationToken))!;
    }

    public async ValueTask<TResponse> SendAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    TResponse>(
        ushort messageType,
        CancellationToken cancellationToken = default)
    {
        await using var pipe = CreatePipeClient();
        await pipe.ConnectAsync(cancellationToken);

        await PipeProtocol.WriteRequestMessageAsync(
            pipe,
            messageType,
            cancellationToken);

        var responseHeader = await ResponseHeader.ReadAsync(pipe);
        return (await PipeProtocol.ReadResponsePayloadAsync<TResponse>(
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
