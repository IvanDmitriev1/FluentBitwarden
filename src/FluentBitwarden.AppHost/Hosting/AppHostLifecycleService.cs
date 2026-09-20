using FluentBitwarden.AppHost.Hosting.Tray;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using Microsoft.Extensions.Hosting;
using FluentBitwarden.AppHost.Hosting.Cli;
using FluentBitwarden.AppHost.Hosting.Handlers;
using Microsoft.Windows.AppLifecycle;

namespace FluentBitwarden.AppHost.Hosting;

internal sealed class AppHostLifecycleService : IHostedService
{
    private readonly TaskCompletionSource<TrayWindow> _messageLoopStarted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource _messageLoopStopped =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IUiProcessLauncher _uiProcessLauncher;

    private readonly AppTrayCommandsHandler _tryAppTrayCommandsHandler;
    private readonly AppInitializer _appInitializer;
    private readonly AppActivationHandler _activationHandler;

    private readonly AppActivationArguments _initialActivationArguments;
    private readonly Thread _messageLoopThread;


    public AppHostLifecycleService(
        IHostApplicationLifetime applicationLifetime,
        IDatabaseInitializationService databaseInitializationService,
        IUiProcessLauncher uiProcessLauncher,
        AppActivationArguments initialActivationArguments)
    {
        _applicationLifetime = applicationLifetime;
        _uiProcessLauncher = uiProcessLauncher;
        _initialActivationArguments = initialActivationArguments;

        _activationHandler = new AppActivationHandler(uiProcessLauncher);
        _appInitializer = new AppInitializer(databaseInitializationService);
        _tryAppTrayCommandsHandler = new AppTrayCommandsHandler(uiProcessLauncher, applicationLifetime);

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

        _appInitializer.Initialize();
        _activationHandler.Handle(_initialActivationArguments.ToCliCommand());
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var trayWindow = await _messageLoopStarted.Task.WaitAsync(cancellationToken);
        trayWindow.RequestShutdown();

        await _messageLoopStopped.Task.WaitAsync(cancellationToken);

        _uiProcessLauncher.Exit();
    }

    public void HandleAppActivation(AppActivationArguments arguments) =>
        _activationHandler.Handle(arguments.ToCliCommand());

    private void MessageLoopThread()
    {
        try
        {
            using var trayWindow = new TrayWindow("FluentBitwarden.AppHost", _tryAppTrayCommandsHandler);
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
}
