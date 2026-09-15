using AsyncAwaitBestPractices;
using FluentBitwarden.Views;
using FluentBitwarden.Platform.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;
using CommunityToolkit.WinUI;
using FluentBitwarden.Application;
using FluentBitwarden.Application.Abstractions;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using FluentBitwarden.Infrastructure.UiCommand;

namespace FluentBitwarden;

public partial class App
{
    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;
    public DispatcherQueue DispatcherQueue { get; }

    private readonly IServiceProvider _services;
    private readonly AppActivationArguments _initialActivation;

    public T GetRequiredService<T>() where T : notnull => _services.GetRequiredService<T>();

    public App(AppActivationArguments initialActivation)
    {
        InitializeComponent();
        TrimmingConfiguration.Preserve();

        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _initialActivation = initialActivation;

        var services = new ServiceCollection()
            .AddAppLogging("ui")
            .AddViews()
            .AddUiServices()
            .AddApplicationServices();
#if DEBUG
        _services = services.BuildServiceProvider(true);
#else
        _services = services.BuildServiceProvider();
#endif

        WireExceptionLogging(_services.GetRequiredService<ILoggerFactory>().CreateLogger("FluentBitwarden.Ui"));
    }

    private void WireExceptionLogging(ILogger logger)
    {
        SafeFireAndForgetExtensions.SetDefaultExceptionHandling(logger.UnhandledException);

        UnhandledException += (_, args) => logger.UnhandledException(args.Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            logger.UnobservedTaskException(args.Exception);
            args.SetObserved();
        };
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        HandleActivation(_initialActivation);
    }

    public void HandleActivation(AppActivationArguments args)
    {
        var command = UiActivationCommandParser.From(args);
        DispatcherQueue.EnqueueAsync(() => GetRequiredService<IAppCoordinator>().HandleActivation(command));
    }
}
