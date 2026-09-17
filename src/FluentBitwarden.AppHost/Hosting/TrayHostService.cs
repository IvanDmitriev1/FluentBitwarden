using FluentBitwarden.AppHost.Hosting.Tray;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.AppHost.Hosting;

internal sealed class TrayHostService : IHostedService
{
    private readonly TaskCompletionSource<TrayWindow> _messageLoopStarted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource _messageLoopStopped =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly Thread _messageLoopThread;

    public TrayHostService(IHostApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
        _messageLoopThread = new Thread(MessageLoopThread)
        {
            IsBackground = false,
            Name = "FluentBitwarden AppHost message loop"
        };

        _messageLoopThread.SetApartmentState(ApartmentState.STA);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _messageLoopThread.Start();

        await _messageLoopStarted.Task.WaitAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var trayWindow = await _messageLoopStarted.Task.WaitAsync(cancellationToken);
        trayWindow.RequestShutdown();

        await _messageLoopStopped.Task.WaitAsync(cancellationToken);
    }

    private void MessageLoopThread()
    {
        try
        {
            using var trayWindow = new TrayWindow("FluentBitwarden.AppHost", LeftButtonHandler, RightButtonHandler);
            _messageLoopStarted.SetResult(trayWindow);
            trayWindow.Run();

            _messageLoopStopped.TrySetResult();
        }
        catch (Exception e)
        {
            _messageLoopStarted.TrySetException(e);
            _messageLoopStopped.TrySetException(e);

            _applicationLifetime.StopApplication();
        }
    }

    private void LeftButtonHandler()
    {
        
    }

    private void RightButtonHandler(TrayMenuCommand command)
    {
        switch (command)
        {
            case TrayMenuCommand.Show:
                //_uiProcessLauncher.ActivateMainWindow();
                return;

            case TrayMenuCommand.Lock:
                //_sessionManager.LockAsync().SafeFireAndForget();
                return;

            case TrayMenuCommand.Exit:
                //_uiProcessLauncher.Exit();
                _applicationLifetime.StopApplication();
                return;
        }
    }

}
