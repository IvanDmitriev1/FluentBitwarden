using FluentBitwarden.CommandPalette.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;

namespace FluentBitwarden.CommandPalette;

internal sealed partial class CommandPaletteComServer(
    FluentBitwardenCommandPaletteExtension extension,
    IAppHostProcessManager processManager) : IHostedService, IDisposable
{
    private ComServer? _server;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        processManager.Activate();

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
