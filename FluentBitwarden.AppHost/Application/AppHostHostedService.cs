using FluentBitwarden.AppHost.Application.Tray;
using FluentBitwarden.AppHost.Application.Sessions;
using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.AppHost.Application;

internal sealed class AppHostHostedService(
    IHostApplicationLifetime applicationLifetime,
    IUiProcessLauncher uiProcessLauncher,
    IVaultSessionCoordinator vaultSessionCoordinator) : IHostedService
{
    private readonly ManualResetEventSlim _messageLoopStarted = new();
    private readonly ManualResetEventSlim _messageLoopStopped = new();

    private Thread? _messageLoopThread;
    private TrayHost? _trayHost;
    private Exception? _startupException;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _messageLoopThread = new Thread(RunMessageLoop)
        {
            IsBackground = false,
            Name = "FluentBitwarden AppHost message loop"
        };

        _messageLoopThread.SetApartmentState(ApartmentState.STA);
        _messageLoopThread.Start();

        try
        {
            _messageLoopStarted.Wait(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return _startupException is null
            ? Task.CompletedTask
            : Task.FromException(_startupException);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _trayHost?.RequestShutdown();

        if (_messageLoopThread is null || ReferenceEquals(Thread.CurrentThread, _messageLoopThread) || _messageLoopStopped.IsSet)
        {
            return Task.CompletedTask;
        }

        try
        {
            _messageLoopStopped.Wait(cancellationToken);
            return Task.CompletedTask;
        }
        catch (OperationCanceledException)
        {
            return Task.FromCanceled(cancellationToken);
        }
    }

    private void RunMessageLoop()
    {
        try
        {
            _trayHost = new TrayHost(applicationLifetime, uiProcessLauncher, vaultSessionCoordinator);
            _messageLoopStarted.Set();

            while (PInvoke.GetMessage(out var message, default, 0, 0).Value > 0)
            {
                PInvoke.TranslateMessage(message);
                PInvoke.DispatchMessage(message);
            }
        }
        catch (Exception exception)
        {
            _startupException = exception;
            _messageLoopStarted.Set();
        }
        finally
        {
            _trayHost?.Dispose();
            _messageLoopStopped.Set();
            applicationLifetime.StopApplication();
        }
    }
}
