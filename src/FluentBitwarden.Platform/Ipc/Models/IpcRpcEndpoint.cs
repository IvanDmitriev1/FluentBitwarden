using System.IO.Pipes;

namespace FluentBitwarden.Platform.Ipc.Models;

internal abstract class IpcRpcEndpoint(IpcRpcHandlerMethodDescriptor descriptor)
{
    public ushort MessageType { get; } = descriptor.MessageType;

    public IpcAuthenticationLevel AuthenticationLevel { get; } = descriptor.AuthenticationLevel;

    public async Task<IpcRpcInvocationResult> InvokeAsync(
        IServiceProvider requestServices,
        NamedPipeServerStream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<bool>? disconnectTask = null;
        IpcRpcInvocationResult result;

        try
        {
            if (stream.HasDisconnectedOrUnexpectedData())
                return IpcRpcInvocationResult.NoResponse;

            Task<byte[]> invocation = InvokeCoreAsync(requestServices, stream, payload, requestCts.Token);
            if (!invocation.IsCompleted)
                disconnectTask = stream.MonitorDisconnectAsync(requestCts);

            result = IpcRpcInvocationResult.Success(await invocation);

            if (disconnectTask is null && stream.HasDisconnectedOrUnexpectedData())
                result = IpcRpcInvocationResult.NoResponse;
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested || requestCts.IsCancellationRequested)
        {
            result = IpcRpcInvocationResult.NoResponse;
        }
        catch (Exception exception)
        {
            result = IpcRpcInvocationResult.Failure(exception);
        }
        finally
        {
            requestCts.Cancel();
            if (disconnectTask is not null && await disconnectTask && !cancellationToken.IsCancellationRequested)
                result = IpcRpcInvocationResult.NoResponse;
        }

        return result;
    }

    protected abstract Task<byte[]> InvokeCoreAsync(
        IServiceProvider requestServices,
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken);
}
