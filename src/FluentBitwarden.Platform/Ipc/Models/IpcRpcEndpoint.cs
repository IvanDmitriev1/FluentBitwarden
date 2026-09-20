namespace FluentBitwarden.Platform.Ipc.Models;

internal abstract class IpcRpcEndpoint(IpcRpcHandlerMethodDescriptor descriptor)
{
    public ushort MessageType { get; } = descriptor.MessageType;

    public IpcAuthenticationLevel AuthenticationLevel { get; } = descriptor.AuthenticationLevel;

    public async ValueTask InvokeAsync(
        IServiceProvider requestServices,
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var disconnectTask = stream.MonitorDisconnectAsync(requestCts);

        try
        {
            await InvokeCoreAsync(requestServices, stream, payload, requestCts.Token);
            await stream.FlushAsync(requestCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            requestCts.Cancel();
            await disconnectTask;
        }
    }

    protected abstract ValueTask InvokeCoreAsync(
        IServiceProvider requestServices,
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken);
}
