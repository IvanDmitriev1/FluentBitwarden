using Microsoft.Extensions.Hosting;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;

namespace FluentBitwarden.CommandPalette.Infrastructure;

internal sealed class CommandPaletteComServer(FluentBitwardenCommandPaletteExtension extension) : IHostedService, IDisposable
{
    private ComServer? _server;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        FluentBitwardenProcessLauncher.EnsureAppHostRunning();

        _server = new ComServer();
        _server.RegisterClass<FluentBitwardenCommandPaletteExtension, IExtension>(() => extension);
        _server.Start();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Stop();
        return Task.CompletedTask;
    }

    public void Dispose() => Interlocked.Exchange(ref _server, null)?.UnsafeDispose();
}
