using FluentBitwarden.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using WinUI.DependencyInjection;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using FluentBitwarden.Application;
using FluentBitwarden.Infrastructure.UiCommand;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : IXamlMetadataServiceProvider
{
    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;
    public DispatcherQueue DispatcherQueue { get; }

    private readonly IServiceProvider _services;
    private readonly AppActivationArguments _initialActivation;

    public object GetRequiredService(Type type)
        => _services.GetRequiredService(type);

    public T GetRequiredService<T>() where T : notnull => _services.GetRequiredService<T>();

    public App(AppActivationArguments initialActivation)
    {
        InitializeComponent();
        TrimmingConfiguration.Preserve();

        UnhandledException += static (_, args) => UnhandledExceptionLogger.WriteException(args.Exception);
        TaskScheduler.UnobservedTaskException += static (_, args) =>
        {
            Debug.WriteLine(args.Exception.Message);
            UnhandledExceptionLogger.WriteException(args.Exception);
            args.SetObserved();
        };

        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _initialActivation = initialActivation;

        var services = new ServiceCollection()
            .AddViews()
            .AddUiServices();
#if DEBUG
        _services = services.BuildServiceProvider(true);
#else
        _services = services.BuildServiceProvider();
#endif
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        HandleActivation(_initialActivation);

        GetRequiredService<IUiHostedServiceManager>().EnsureProcessServicesStarted();
    }

    public void HandleActivation(AppActivationArguments args)
    {
        var command = UiActivationCommandParser.From(args);
        bool result = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High,
            () => GetRequiredService<AppCoordinator>().HandleActivation(command));

        Debug.WriteLine(result);
    }
}
