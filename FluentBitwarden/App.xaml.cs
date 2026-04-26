using BitwardenApi;
using FluentBitwarden.Application.Diagnostics;
using FluentBitwarden.Application.Lifetime;
using FluentBitwarden.Application.Tray;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Account;
using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Modules.Security;
using FluentBitwarden.Modules.Session;
using FluentBitwarden.Modules.Session.Services.Authentication;
using FluentBitwarden.Modules.Vault;
using FluentBitwarden.Shared.Connectivity;
using FluentBitwarden.Shared.Extensions;
using FluentBitwarden.Shared.SiteIcons;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;
using FluentBitwarden.Shared.Ipc;
using FluentBitwarden.Shared.Ipc.Abstractions;
using WinUI.DependencyInjection;
using WinUIEx;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : IXamlMetadataServiceProvider
{
    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    public DispatcherQueue DispatcherQueue { get; private set; }

    private readonly SimpleSplashScreen _fss;
    private readonly AppActivationArguments _initialActivation;

    public IHost Host { get; } = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((ctx, services) =>
        {
            services.AddHttpClient<ISiteIconCache, SiteIconCache>(client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");
            });

            services.AddSingleton<IAppActivationService, AppActivationService>();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            services.AddSingleton<IAppRestartService, AppRestartService>();
            services.AddSingleton<IMainWindowService, MainWindowService>();

            services.AddNamedPipeIpc();
            services.AddShellServices();
            services.AddViews();
            services.AddDatabaseServices();
            services.AddConnectivityModule();

            services.AddBitwardenApi<BearerTokenHandler>();
            services.AddAccountModule();
            services.AddSecurityModule();
            services.AddSessionModule();
            services.AddAppStateModule();
            services.AddVaultServices();

        })
        .Build();

    public object GetRequiredService(Type type)
        => Host.Services.GetRequiredService(type);

    public T GetRequiredService<T>() where T : notnull => Host.Services.GetRequiredService<T>();

    public App(SimpleSplashScreen fss, AppActivationArguments initialActivation)
    {
        InitializeComponent();

        _fss = fss;
        _initialActivation = initialActivation;

        UnhandledException += static (sender, args) => UnhandledExceptionLogger.WriteException(args.Exception);
        DispatcherQueue = DispatcherQueue.GetForCurrentThread();

        ValidationTrimDependencies.Preserve();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif

        await Host.Services.GetRequiredService<IAppFirstRunService>().InitializeAsync();
        _ = Task.Run(() => Host.Services.GetRequiredService<IIpcPipeServer>().RunAsync());

        _fss.Dispose();

        await Host.Services.GetRequiredService<IAppActivationService>().InitializeAsync(_initialActivation);
        Host.Services.GetRequiredService<ITrayIconService>().EnsureCreated();
    }

    public void HandleActivation(AppActivationArguments args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _ = Host.Services.GetRequiredService<IAppActivationService>().HandleAsync(args);
        });
    }
}
