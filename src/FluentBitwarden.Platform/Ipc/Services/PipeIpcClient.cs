using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Platform.Ipc.Transport;
using System.IO.Pipes;
using System.Runtime.ExceptionServices;

namespace FluentBitwarden.Platform.Ipc.Services;

internal sealed class PipeIpcClient(
    string pipeName,
    IIpcExceptionHandler exceptionHandler) : IIpcClient
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

    private async Task<TResponse> ReadResponseAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    TResponse>(
        Stream pipe,
        CancellationToken cancellationToken)
    {
        var responseHeader = await IpcRpcResponseHeader.ReadAsync(pipe, cancellationToken);
        if (responseHeader.IsSuccessful)
        {
            return (await IpcWireProtocol.ReadRpcResponsePayloadAsync<TResponse>(
                pipe,
                responseHeader.PayloadLength,
                cancellationToken))!;
        }

        IpcRpcFailureResponse failure = await IpcWireProtocol.ReadRpcFailureResponseAsync(
            pipe,
            responseHeader.PayloadLength,
            cancellationToken);

        Exception exception = failure.Code switch
        {
            IpcRpcFailureCode.LockedSession => new UnlockedSessionRequiredException(),
            IpcRpcFailureCode.Cancellation => new OperationCanceledException(cancellationToken),
            IpcRpcFailureCode.Generic => new IpcRemoteException(),
            _ => new InvalidDataException("The IPC failure code is invalid."),
        };

        try
        {
            exceptionHandler.Handle(exception);
        }
        catch (Exception)
        {
            // Notification failures must not replace the remote RPC failure.
        }

        ExceptionDispatchInfo.Capture(exception).Throw();
        throw new UnreachableException();
    }

    private NamedPipeClientStream CreatePipeClient() => new(
        ".",
        pipeName,
        PipeDirection.InOut,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
}
