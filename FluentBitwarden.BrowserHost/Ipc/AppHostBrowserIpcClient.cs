using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.BrowserHost.Ipc;

internal sealed class AppHostBrowserIpcClient(IIpcClient ipcClient)
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(2);

    public async ValueTask<TResponse> SendAsync<TRequest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IIpcRequestMessage
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);

        try
        {
            return await ipcClient.SendAsync<TRequest, TResponse>(request, timeout.Token);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AppHostBrowserIpcException(
                "apphost_unavailable",
                "FluentBitwarden AppHost is unavailable.",
                exception);
        }
        catch (Exception exception) when (exception is IOException or EndOfStreamException)
        {
            throw new AppHostBrowserIpcException(
                "apphost_unavailable",
                "FluentBitwarden AppHost is unavailable.",
                exception);
        }
        catch (Exception exception)
        {
            throw new AppHostBrowserIpcException(
                "apphost_error",
                "FluentBitwarden AppHost could not process the browser request.",
                exception);
        }
    }
}
