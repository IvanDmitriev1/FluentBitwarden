using BitwardenApi;
using FluentBitwarden.Application.Diagnostics;
using FluentBitwarden.Application.Lifetime;
using FluentBitwarden.Application.Tray;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Account;
using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.Security;
using FluentBitwarden.Modules.Session;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;
using FluentBitwarden.Shared.Extensions;
using FluentBitwarden.Views;
using FluentBitwarden.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Windows.Storage;
using CommunityToolkit.Helpers;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.AppState.Abstractions;
using WinUI.DependencyInjection;
using WinUIEx;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : IXamlMetadataServiceProvider
{
    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SimpleSplashScreen _fss;

    public IHost Host { get; } = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((ctx, services) =>
        {
            services.AddSingleton<IAppActivationService, AppActivationService>();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            services.AddSingleton<IAppRestartService, AppRestartService>();

            services.AddShellServices();
            services.AddViews();
            services.AddDatabaseServices();

            services.AddBitwardenApi<BearerTokenHandler>();
            services.AddAccountModule();
            services.AddSecurityModule();
            services.AddSessionModule();
            services.AddAppStateModule();
        })
        .Build();

    public object GetRequiredService(Type type)
        => Host.Services.GetRequiredService(type);

    public App(SimpleSplashScreen fss)
    {
        InitializeComponent();

        _fss = fss;

        UnhandledException += static (sender, args) => UnhandledExceptionLogger.WriteException(args.Exception);
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        ValidationTrimDependencies.Preserve();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }

        _ = Host.Services.GetRequiredService<ISettingsService>().Get();
        await Host.Services.GetRequiredService<IDataInitializationService>().InitializeAsync();

        _fss.Hide();
        _fss.Dispose();

        Host.Services.GetRequiredService<IAppActivationService>().Activate(args);
        Host.Services.GetRequiredService<ITrayIconService>().EnsureCreated();
    }

    public void ReopenWindow()
    {
        _dispatcherQueue.TryEnqueue(() => Host.Services.GetRequiredService<IAppActivationService>().ReopenMainWindow());
    }
}
