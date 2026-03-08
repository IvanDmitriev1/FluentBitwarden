using BitwaredApi;
using FluentBitwarden.Extentions;
using FluentBitwarden.Ui.Abstractions;
using FluentBitwarden.Ui.Extensions;
using FluentBitwarden.Ui.Navigation;
using FluentBitwarden.ViewModels;
using FluentBitwarden.ViewModels.SetUp;
using FluentBitwarden.Views;
using FluentBitwarden.Views.SetUp;
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
                services.AddBitwaredPlatformServices();

                services.AddBitwaredApi(options =>
                {
                    options.Environment = BitwardenEnvironment.UnitedStates;
                });

                services.AddSingleton<MainWindow>();
                services.AddSingleton<INavigationService, FrameNavigationService>();

                services.AddView<LoginPage, LoginPageViewModel>();
                services.AddView<SetupPage, SetupPageViewModel>();
                services.AddView<VaultPage, VaultPageViewModel>();
            })
            .Build();

    public object GetRequiredService(Type type)
        => Host.Services.GetRequiredService(type);

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _mainWindow ??= Host.Services.GetRequiredService<MainWindow>();
        _mainWindow.Activate();

        Host.Services.GetRequiredService<INavigationService>().Navigate(typeof(LoginPage));
    }
}
