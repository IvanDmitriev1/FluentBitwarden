using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules;

namespace FluentBitwarden.Platform.AppHost.Lifetime;

internal sealed class AppHostLifetimeService(IIpcClient ipcClient) : IAppHostLifetimeService
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(0.5);

    public async Task ShutdownAppHostAsync(CancellationToken cancellationToken = default)
    {
        using var shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        shutdownCts.CancelAfter(ShutdownTimeout);

        try
        {
            await ipcClient.SendAsync<IpcVoid>(IpcMessageTypes.System.ShutdownAppHost, shutdownCts.Token);
        }
        catch
        {
            //
        }
    }
}
