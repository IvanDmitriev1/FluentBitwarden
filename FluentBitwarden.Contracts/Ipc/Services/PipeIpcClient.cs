using FluentBitwarden.Contracts.Ipc.Internal;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;

namespace FluentBitwarden.Contracts.Ipc.Services;

internal sealed class PipeIpcClient(string pipeName) : IIpcClient
{
    public async ValueTask<IpcResult<TResponse>> SendAsync<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IIpcRequestMessage
        where TResponse : notnull
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

        var responseHeader = ResponseHeader.Read(pipe);
        return await PipeProtocol.ReadPayloadAsync<IpcResult<TResponse>>(
            pipe,
            responseHeader.PayloadLength,
            cancellationToken);
    }

    public async ValueTask<IpcResult<TResponse>> SendAsync<TResponse>(ushort messageType, CancellationToken cancellationToken = default) where TResponse : notnull
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

        var responseHeader = ResponseHeader.Read(pipe);

        return await PipeProtocol.ReadPayloadAsync<IpcResult<TResponse>>(
            pipe,
            responseHeader.PayloadLength,
            cancellationToken);
    }
}
