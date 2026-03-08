using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Ui.Extensions;
using FluentBitwarden.Ui.Navigation;
using FluentBitwarden.ViewModels;
using FluentBitwarden.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using WinUI.DependencyInjection;

namespace FluentBitwarden;

[XamlMetadataServiceProvider]
public partial class App : Application, IXamlMetadataServiceProvider
{
    private MainWindow? _mainWindow;

    public IHost Host { get; } =
        Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddSingleton<MainWindow>();
                services.AddSingleton<INavigationService, FrameNavigationService>();

                services.AddView<SetupPage, SetupPageViewModel>();
                services.AddTransient<VaultPage>();
            })
            .Build();

    public App()
    {
        InitializeComponent();
    }

    public object GetRequiredService(Type type)
        => Host.Services.GetRequiredService(type);

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _mainWindow ??= Host.Services.GetRequiredService<MainWindow>();
        _mainWindow.Activate();
    }
}
