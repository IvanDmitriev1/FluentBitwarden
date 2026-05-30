using FluentBitwarden.Contracts.Ipc.Internal;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;

namespace FluentBitwarden.Contracts.Ipc.Services;

internal sealed class PipeIpcClient(string pipeName) : IIpcClient
{
    public async ValueTask<TResponse> SendAsync<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IIpcRequestMessage
    {
        await using var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

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

    public async ValueTask<TResponse> SendAsync<TResponse>(ushort messageType, CancellationToken cancellationToken = default)
    {
        await using var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

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
}
