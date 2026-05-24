using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.AppHost;
using FluentBitwarden.Application.Diagnostics;
using FluentBitwarden.Application.Lifetime;
using FluentBitwarden.Infrastructure.Extensions;
using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Modules.Passkey.Abstractions;
using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;
using WinUI.DependencyInjection;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Implementations;
using FluentBitwarden.Infrastructure;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : IXamlMetadataServiceProvider
{
    private readonly AppActivationArguments _initialActivation;
    private const string ExitCommandArgument = "--exit";

    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    public DispatcherQueue DispatcherQueue { get; }

    public IHost Host { get; } = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((ctx, services) =>
        {
            services.AddFluentBitwardenApplicationServices();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IThemeService>(static sp => sp.GetRequiredService<MainWindow>());
            services.AddSingleton<IMessenger>(StrongReferenceMessenger.Default);
            services.AddTransient<IPasskeyOverlayService, PasskeyOverlayService>();
            services.AddTransient<ISshUserActionPrompt, SshUserActionPrompt>();

            services.AddViews();
            services.AddUiServices();
        })
        .Build();

    public object GetRequiredService(Type type)
        => Host.Services.GetRequiredService(type);

    public T GetRequiredService<T>() where T : notnull => Host.Services.GetRequiredService<T>();

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

        Host.Services.GetRequiredService<IAppSetupService>().Initialize();
        _ = Task.Run(() => Host.Services.GetRequiredService<IIpcPipeServer>().RunAsync());
        _ = Task.Run(() => Host.Services.GetRequiredService<ISshAgentServer>().RunAsync());
    }

    public void HandleActivation(AppActivationArguments args)
    {
        var lunchArgs = (ILaunchActivatedEventArgs)args.Data;
        var parameters = lunchArgs.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parameters.Length <= 1)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, MainWindow.Instance.ShowWindow);
            return;
        }

        switch (parameters[1])
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
