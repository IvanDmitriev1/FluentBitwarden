using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.Platform.Ipc.Internal;

internal delegate ValueTask<TResponse> IpcRequestHandlerDelegate<in TRequest, TResponse>(
    TRequest request,
    CancellationToken cancellationToken)
    where TRequest : IIpcRequestMessage;

internal delegate ValueTask IpcRequestHandlerDelegate<in TRequest>(
    TRequest request,
    CancellationToken cancellationToken)
    where TRequest : IIpcRequestMessage;

internal delegate ValueTask<TResponse> IpcCommandHandlerDelegate<TResponse>(
    CancellationToken cancellationToken);

internal delegate ValueTask IpcCommandHandlerDelegate(
    CancellationToken cancellationToken);