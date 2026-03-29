using BitwardenApi;
using FluentBitwarden.Application.Diagnostics;
using FluentBitwarden.Application.Lifetime;
using FluentBitwarden.Application.Tray;
using FluentBitwarden.Data;
using FluentBitwarden.Modules.Account;
using FluentBitwarden.Modules.Session;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Services;
using FluentBitwarden.Shared.Extensions;
using FluentBitwarden.Shell;
using FluentBitwarden.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using WinUI.DependencyInjection;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : IXamlMetadataServiceProvider
{
    public new static App Current => (App)Microsoft.UI.Xaml.Application.Current;

    private readonly DispatcherQueue _dispatcherQueue;

    public IHost Host { get; } = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((ctx, services) =>
        {
            services.AddSingleton<IAppActivationService, AppActivationService>();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            services.AddSingleton<IAppRestartService, AppRestartService>();

            services.AddShellServices();
            services.AddViews();
            services.AddDataServices();

            services.AddBitwardenApi<BearerTokenHandler>();
            services.AddAccountModule();
            services.AddSessionModule();
        })
        .Build();

    public object GetRequiredService(Type type)
        => Host.Services.GetRequiredService(type);

    public App()
    {
        InitializeComponent();

        ValidationTrimDependencies.Preserve();
        UnhandledException += static (sender, args) => UnhandledExceptionLogger.WriteException(args.Exception);
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _ = Host.Services.GetRequiredService<ISessionTokensStore>();
        Host.Services.GetRequiredService<IAppActivationService>().Activate(args);
        Host.Services.GetRequiredService<ITrayIconService>().EnsureCreated();

        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
    }

    public void ReopenWindow()
    {
        _dispatcherQueue.TryEnqueue(() => Host.Services.GetRequiredService<IAppActivationService>().ReopenMainWindow());
    }
}
