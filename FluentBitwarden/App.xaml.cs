using BitwardenApi;
using CommunityToolkit.Mvvm.Messaging;
using FluentBitwarden.Application.Diagnostics;
using FluentBitwarden.Application.Lifetime;
using FluentBitwarden.Data;
using FluentBitwarden.Infrastructure.Extensions;
using FluentBitwarden.Infrastructure.Ipc;
using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Services;
using FluentBitwarden.Modules.Account;
using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Modules.Passkey;
using FluentBitwarden.Modules.Session;
using FluentBitwarden.Modules.Session.Services;
using FluentBitwarden.Modules.SshAgent;
using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Modules.Vault;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using WinUI.DependencyInjection;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : IXamlMetadataServiceProvider
{
    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    public DispatcherQueue DispatcherQueue { get; }

    private readonly AppActivationArguments _initialActivation;

    public IHost Host { get; } = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((ctx, services) =>
        {
            services.AddTransient<IAppSetupService, AppSetupService>();

            services.AddSingleton<MainWindow>();
            services.AddSingleton<IThemeService>(static sp => sp.GetRequiredService<MainWindow>());
            services.AddSingleton<IMessenger>(StrongReferenceMessenger.Default);

            services.AddNamedPipeIpc();
            services.AddViews();
            services.AddDatabaseServices();
            services.AddSharedServices();

            services.AddBitwardenApi<BearerAuthTokenProvider>();
            services.AddAccountModule();
            services.AddSessionModule();
            services.AddVaultServices();
            services.AddPasskeyModule();
            services.AddSshAgent();

        })
        .Build();

    public object GetRequiredService(Type type)
        => Host.Services.GetRequiredService(type);

    public T GetRequiredService<T>() where T : notnull => Host.Services.GetRequiredService<T>();

    public App(AppActivationArguments initialActivation)
    {
        InitializeComponent();

        _initialActivation = initialActivation;

        UnhandledException += static (sender, args) => UnhandledExceptionLogger.WriteException(args.Exception);
        TaskScheduler.UnobservedTaskException += static (sender, args) =>
        {
            Debug.WriteLine(args.Exception.Message);
            UnhandledExceptionLogger.WriteException(args.Exception);
            args.SetObserved();
        };

        ValidationTrimDependencies.Preserve();

        DispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Host.Services.GetRequiredService<IAppSetupService>().Initialize();
        _ = Task.Run(() => Host.Services.GetRequiredService<IIpcPipeServer>().RunAsync());
        _ = Task.Run(() => Host.Services.GetRequiredService<ISshAgentServer>().RunAsync());

        AppLifetimeManager.Activate(_initialActivation);
    }

    public void HandleActivation(AppActivationArguments args)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () => AppLifetimeManager.Activate(args));
    }
}
