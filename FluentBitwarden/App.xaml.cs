using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Extensions;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using FluentBitwarden.Infrastructure;
using WinUI.DependencyInjection;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using FluentBitwarden.Contracts.Infrastructure.Shared;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : IXamlMetadataServiceProvider
{
    private readonly AppActivationArguments _initialActivation;

    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    public DispatcherQueue DispatcherQueue { get; }

    private IServiceProvider Services = new ServiceCollection()
        .AddSingleton<MainWindow>()
        .AddSingleton<IThemeService>(static sp => sp.GetRequiredService<MainWindow>())
        .AddSingleton<IMessenger>(StrongReferenceMessenger.Default)

        .AddViews()
        .AddInfrastructureServices()
        .BuildServiceProvider();

    public object GetRequiredService(Type type)
        => Services.GetRequiredService(type);

    public T GetRequiredService<T>() where T : notnull => Services.GetRequiredService<T>();

    public App(AppActivationArguments initialActivation)
    {
        InitializeComponent();
        ValidationTrimDependencies.Preserve();

        UnhandledException += static (_, args) => UnhandledExceptionLogger.WriteException(args.Exception);
        TaskScheduler.UnobservedTaskException += static (_, args) =>
        {
            Debug.WriteLine(args.Exception.Message);
            UnhandledExceptionLogger.WriteException(args.Exception);
            args.SetObserved();
        };

        DispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _initialActivation = initialActivation;
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _ = GetRequiredService<MainWindow>();
        HandleActivation(_initialActivation);
    }

    public void HandleActivation(AppActivationArguments args)
    {
        var lunchArgs = (ILaunchActivatedEventArgs)args.Data;
        var parameters = lunchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1);
        var firstParameter = parameters.FirstOrDefault();

        switch (firstParameter)
        {
            case "--exit":
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, Exit);
                break;
            default:
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, MainWindow.Instance.ShowWindow);
                break;
        }
    }
}
