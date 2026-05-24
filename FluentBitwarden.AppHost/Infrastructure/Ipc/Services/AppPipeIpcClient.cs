using System.IO.Pipes;
using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Internal;

namespace FluentBitwarden.Infrastructure.Ipc.Services;

[Fody.ConfigureAwait(false)]
internal sealed class AppPipeIpcClient : IAppPipeIpcClient
{
    public async ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IPipeRequestMessage
        where TResponse : notnull
    {
        await using var pipe = new NamedPipeClientStream(
            ".",
            IpcConstants.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        await pipe.ConnectAsync(cancellationToken);

        await PipeProtocol.WriteRequestMessageAsync(
            pipe,
            TRequest.MessageType,
            request,
            cancellationToken);

        var responseHeader = ResponseHeader.Read(pipe);
        return await PipeProtocol.ReadPayloadAsync<TResponse>(
            pipe,
            responseHeader.PayloadLength,
            cancellationToken);
    }
}
