namespace FluentBitwarden.Contracts.Ipc.Internal;

internal delegate ValueTask<TResponse?> IpcRequestHandlerDelegate<in TRequest, TResponse>(
    TRequest request,
    CancellationToken cancellationToken)
    where TRequest : IIpcRequestMessage
    where TResponse : notnull;

internal delegate ValueTask IpcRequestHandlerDelegate<in TRequest>(
    TRequest request,
    CancellationToken cancellationToken)
    where TRequest : IIpcRequestMessage;

internal delegate ValueTask<TResponse?> IpcCommandHandlerDelegate<TResponse>(
    CancellationToken cancellationToken)
    where TResponse : notnull;

internal delegate ValueTask IpcCommandHandlerDelegate(
    CancellationToken cancellationToken);