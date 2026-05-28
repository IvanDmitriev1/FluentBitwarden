using FluentBitwarden.Contracts.Ipc.Internal;
using FluentBitwarden.Contracts.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Contracts.Ipc.Services;

public sealed class PipeRequestHandlerInvoker<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    TRequest, TResponse>(IServiceScopeFactory scopeFactory, Delegate handler) : IIpcRequestHandlerInvoker
    where TRequest : IIpcRequestMessage
    where TResponse : notnull
{
    private readonly IpcRequestHandlerDelegate<TRequest, TResponse> _handler =
        IpcRequestHandlerDelegateFactory.Create<TRequest, TResponse>(handler);

    public ushort MessageType { get; } = TRequest.MessageType;

    public async ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken)
    {
        var request = await PipeProtocol.ReadPayloadAsync<TRequest>(stream, payloadLength, cancellationToken);

        await using var scope = scopeFactory.CreateAsyncScope();
        var response = await _handler.Invoke(request, scope.ServiceProvider, cancellationToken);
        await PipeProtocol.WriteResponseMessageAsync(stream, response, cancellationToken);
    }
}

public sealed class PipeRequestHandlerInvoker<TResponse>(
    IServiceScopeFactory scopeFactory,
    ushort messageType,
    Delegate handler) : IIpcRequestHandlerInvoker where TResponse : notnull
{
    public ushort MessageType { get; } = messageType;

    private readonly IpcRequestHandlerDelegate<TResponse> _handler =
        IpcRequestHandlerDelegateFactory.Create<TResponse>(handler);

    public async ValueTask InvokeAsync(Stream stream, int payloadLength, CancellationToken cancellationToken)
    {
        if (payloadLength != 0)
        {
            throw new InvalidOperationException(
                $"IPC message {MessageType} does not accept a request payload, but received {payloadLength} bytes.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var response = await _handler.Invoke(
            scope.ServiceProvider,
            cancellationToken);

        await PipeProtocol.WriteResponseMessageAsync(
            stream,
            response,
            cancellationToken);
    }
}