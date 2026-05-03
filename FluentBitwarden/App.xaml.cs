using BitwardenApi;
using FluentBitwarden.Application.Diagnostics;
using FluentBitwarden.Application.Lifetime;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Account;
using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Modules.Passkey;
using FluentBitwarden.Modules.Security;
using FluentBitwarden.Modules.Session;
using FluentBitwarden.Modules.Session.Services.Authentication;
using FluentBitwarden.Modules.SshAgent;
using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Modules.Vault;
using FluentBitwarden.Shared.Extensions;
using FluentBitwarden.Shared.Ipc;
using FluentBitwarden.Shared.Ipc.Abstractions;
using FluentBitwarden.Shared.Services;
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
            services.AddNamedPipeIpc();
            services.AddShellServices();
            services.AddViews();
            services.AddDatabaseServices();
            services.AddSharedServices();

            services.AddBitwardenApi<BearerTokenHandler>();
            services.AddAccountModule();
            services.AddSecurityModule();
            services.AddSessionModule();
            services.AddAppStateModule();
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
        Host.Services.GetRequiredService<IAppFirstRunService>().Initialize();
        _ = Task.Run(() => Host.Services.GetRequiredService<IIpcPipeServer>().RunAsync());
        _ = Task.Run(() => Host.Services.GetRequiredService<ISshAgentServer>().RunAsync());

        AppLifetimeManager.Activate(_initialActivation);
    }

    public void HandleActivation(AppActivationArguments args)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () => AppLifetimeManager.Activate(args));
    }
}
