namespace FluentBitwarden.Contracts.Ipc.Internal;

internal delegate ValueTask<TResponse> IpcRequestHandlerDelegate<in TRequest, TResponse>(
    TRequest request,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken)
    where TRequest : IIpcRequestMessage
    where TResponse : notnull;

internal delegate ValueTask<TResponse> IpcRequestHandlerDelegate<TResponse>(
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken)
    where TResponse : notnull;