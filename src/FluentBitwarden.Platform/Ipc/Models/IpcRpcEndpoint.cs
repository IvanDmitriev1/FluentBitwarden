namespace FluentBitwarden.Platform.Ipc.Models;

internal abstract class IpcRpcEndpoint(IpcRpcHandlerMethodDescriptor descriptor)
{
    public ushort MessageType { get; } = descriptor.MessageType;

    public IpcAuthenticationLevel AuthenticationLevel { get; } = descriptor.AuthenticationLevel;

    public async Task<IpcRpcInvocationResult> InvokeAsync(
        IServiceProvider requestServices,
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var disconnectTask = stream.MonitorDisconnectAsync(requestCts);
        IpcRpcInvocationResult result;

        try
        {
            result = IpcRpcInvocationResult.Success(
                await InvokeCoreAsync(requestServices, stream, payload, requestCts.Token));
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
            if (await disconnectTask && !cancellationToken.IsCancellationRequested)
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

internal readonly record struct IpcRpcInvocationResult(
    IpcRpcInvocationStatus Status,
    byte[]? ResponsePayload,
    Exception? Exception)
{
    public static IpcRpcInvocationResult Success(byte[] responsePayload) =>
        new(IpcRpcInvocationStatus.Success, responsePayload, null);

    public static IpcRpcInvocationResult Failure(Exception exception) =>
        new(IpcRpcInvocationStatus.Failure, null, exception);

    public static IpcRpcInvocationResult NoResponse =>
        new(IpcRpcInvocationStatus.NoResponse, null, null);
}

internal enum IpcRpcInvocationStatus
{
    Success,
    Failure,
    NoResponse,
}
