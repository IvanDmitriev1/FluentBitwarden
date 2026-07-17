using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;
using Microsoft.Extensions.Hosting;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;

namespace FluentBitwarden.CommandPalette.Application;

internal sealed partial class CommandPaletteComServer(
    FluentBitwardenCommandPaletteExtension extension,
    IAppHostProcessManager processManager) : IHostedService, IDisposable
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
        Justification = "Disposed via ComServer.UnsafeDispose() in Dispose(), not the standard Dispose() method the analyzer looks for.")]
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
