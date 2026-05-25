using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Internal;

namespace FluentBitwarden.Infrastructure.Ipc.Services;

[Fody.ConfigureAwait(false)]
public sealed class PipeRequestHandlerInvoker<THandler, TRequest, TResponse>(ushort messageType, THandler handler)
    : IPipeRequestHandlerInvoker
    where THandler : class, IPipeRequestHandler<TRequest, TResponse>
    where TRequest : IPipeRequestMessage
    where TResponse : notnull
{
    public ushort MessageType { get; } = messageType;

    public async ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken)
    {
        var request = await PipeProtocol.ReadPayloadAsync<TRequest>(stream, payloadLength, cancellationToken);
        var response = await handler.HandleAsync(request, cancellationToken);

        await PipeProtocol.WriteResponseMessageAsync(stream, response, cancellationToken);
    }
}
