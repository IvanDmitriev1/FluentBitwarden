using AsyncAwaitBestPractices;
using FluentBitwarden.AppHost.Hosting.Activation;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Infrastructure.Tray;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Platform.Infrastructure.Integrations;
using FluentBitwarden.Platform.Settings;
using Microsoft.Extensions.Hosting;
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
    private readonly IDatabaseInitializationService _databaseInitializationService;

    private readonly AppTray _appTray;
    private readonly AppActivationHandler _activationHandler;

    private readonly AppActivationArguments _initialActivationArguments;
    private readonly Thread _messageLoopThread;


    public AppHostLifecycleService(
        IHostApplicationLifetime applicationLifetime,
        IUiProcessLauncher uiProcessLauncher,
        IDatabaseInitializationService databaseInitializationService,
        AppActivationHandler activationHandler,
        AppTray appTray,
        AppActivationArguments initialActivationArguments)
    {
        _applicationLifetime = applicationLifetime;
        _uiProcessLauncher = uiProcessLauncher;
        _databaseInitializationService = databaseInitializationService;
        _initialActivationArguments = initialActivationArguments;
        _activationHandler = activationHandler;
        _appTray = appTray;

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

        Initialize();
        _activationHandler.HandleActivation(_initialActivationArguments);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var trayWindow = await _messageLoopStarted.Task.WaitAsync(cancellationToken);
        trayWindow.RequestShutdown();

        await _messageLoopStopped.Task.WaitAsync(cancellationToken);

        _uiProcessLauncher.Exit();
    }

    private void MessageLoopThread()
    {
        try
        {
            using var trayWindow = new TrayWindow("FluentBitwarden.AppHost", _appTray.Activate, _appTray.CreateMenu);
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

    public void Initialize()
    {
        _databaseInitializationService.Initialize();

        if (SettingsStore.Instance.Get(AppSettingKeys.App.SetupCompletedKey))
        {
            return;
        }

        if (PasskeyPluginSetupService.IsSupported())
        {
            PasskeyPluginSetupService.EnsureRegisteredAsync().SafeFireAndForget();

            SettingsStore.Instance.Set(
                AppSettingKeys.Passkeys.PluginEnabledKey,
                true);
        }

        SettingsStore.Instance.Set(
            AppSettingKeys.App.SetupCompletedKey,
            true);
    }
}
